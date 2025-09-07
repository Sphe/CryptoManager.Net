using CryptoClients.Net.Interfaces;
using CryptoClients.Net.Models;
using CryptoExchange.Net.Authentication;
using CryptoExchange.Net.SharedApis;
using CryptoManager.Net.Caching;
using CryptoManager.Net.Database;
using CryptoManager.Net.Database.Models;
using CryptoManager.Net.Database.Projections;
using CryptoManager.Net.Models.Requests;
using CryptoManager.Net.Models.Response;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using MongoDB.Bson;
using System.Linq.Expressions;

namespace CryptoManager.Net.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [ResponseCache(Duration = 5, Location = ResponseCacheLocation.Client, VaryByQueryKeys = ["*"])]
    [ServerCache(Duration = 5)]
    public class BalancesController : ApiController
    {
        private readonly ILogger _logger;
        private readonly IExchangeUserClientProvider _clientProvider;

        public BalancesController(
            ILogger<BalancesController> logger, 
            IExchangeUserClientProvider clientProvider,
            MongoTrackerContext dbContext) : base(dbContext) 
        {
            _logger = logger;
            _clientProvider = clientProvider;
        }

        [HttpGet("exchange")]
        public async Task<ApiResultPaged<IEnumerable<ApiBalance>>> ListExchangeAsync(
            string? query = null,
            string? asset = null,
            string? exchange = null,
            string? orderBy = null,
            OrderDirection? orderDirection = null,
            int page = 1,
            int pageSize = 20)
        {
            var data = await _dbContext.UserExchangeBalances(int.Parse(UserId), exchange);
            
            var filteredData = data.AsQueryable();
            
            if (!string.IsNullOrEmpty(query))
                filteredData = filteredData.Where(x => x.Asset.Contains(query));

            if (!string.IsNullOrEmpty(asset))
                filteredData = filteredData.Where(x => x.Asset == asset);

            if (string.IsNullOrEmpty(orderBy))
                orderBy = nameof(ApiBalance.UsdValue);

            Expression<Func<ExchangeBalanceValue, object?>> order = orderBy switch
            {
                nameof(ApiBalance.Available) => balance => balance.Available,
                nameof(ApiBalance.Total) => balance => balance.Total,
                nameof(ApiBalance.UsdValue) => balance => balance.UsdValue,
                nameof(ApiBalance.Exchange) => balance => balance.Exchange,
                nameof(ApiBalance.Asset) => balance => balance.Asset,
                _ => throw new ArgumentException(),
            };

            filteredData = orderDirection == OrderDirection.Ascending
                ? filteredData.OrderBy(order)
                : filteredData.OrderByDescending(order);

            var total = filteredData.Count();
            var pageData = filteredData.Skip((page - 1) * pageSize).Take(pageSize).ToList();
            
            return ApiResultPaged<IEnumerable<ApiBalance>>.Ok(page, pageSize, total, pageData.Select(x => new ApiBalance
            {
                Exchange = x.Exchange,
                Asset = x.Asset,
                Available = x.Available,
                Total = x.Total,
                UsdValue = x.UsdValue
            }));
        }

        [HttpGet("external")]
        public async Task<ApiResultPaged<IEnumerable<ApiBalance>>> ListExternalAsync(
            string? query = null,
            string? orderBy = null,
            OrderDirection? orderDirection = null,
            int page = 1,
            int pageSize = 20)
        {
            var data = await _dbContext.UserExternalBalances(int.Parse(UserId));
            
            var filteredData = data.AsQueryable();
            
            if (!string.IsNullOrEmpty(query))
                filteredData = filteredData.Where(x => x.Asset.Contains(query));

            if (string.IsNullOrEmpty(orderBy))
                orderBy = nameof(ApiBalance.UsdValue);

            Expression<Func<ExternalBalanceValue, object?>> order = orderBy switch
            {
                nameof(ApiBalance.Total) => balance => balance.Total,
                nameof(ApiBalance.UsdValue) => balance => balance.UsdValue,
                nameof(ApiBalance.Asset) => balance => balance.Asset,
                _ => throw new ArgumentException(),
            };

            filteredData = orderDirection == OrderDirection.Ascending
                ? filteredData.OrderBy(order)
                : filteredData.OrderByDescending(order);

            var total = filteredData.Count();
            var pageData = filteredData.Skip((page - 1) * pageSize).Take(pageSize).ToList();
            
            return ApiResultPaged<IEnumerable<ApiBalance>>.Ok(page, pageSize, total, pageData.Select(x => new ApiBalance
            {
                Id = x.Id,
                Asset = x.Asset,
                Total = x.Total,
                UsdValue = x.UsdValue
            }));
        }

        [HttpGet("valuation")]
        public async Task<ApiResult<ApiBalanceValuation>> GetValueAsync(string? exchange = null)
        {
            var exchangeData = await _dbContext.UserExchangeBalances(int.Parse(UserId), exchange);
            var externalData = await _dbContext.UserExternalBalances(int.Parse(UserId));
            
            var exchangeValue = exchangeData.Sum(x => x.UsdValue);
            var externalValue = externalData.Sum(x => x.UsdValue);

            return ApiResult<ApiBalanceValuation>.Ok(new ApiBalanceValuation
            {
                Exchange = exchangeValue,
                External = externalValue,
                Total = externalValue + exchangeValue
            });
        }

        [HttpGet("history")]
        public async Task<ApiResult<IEnumerable<ApiUserHistoryItem>>> GetHistoryAsync(string period)
        {
            List<UserValuation> data;
            var filter = Builders<UserValuation>.Filter.And(
                Builders<UserValuation>.Filter.Eq(x => x.UserId, UserId)
            );

            if (period == "1w")
            {
                // One week, return one point per day
                var compareTime = DateTime.UtcNow.AddDays(-8);
                filter = Builders<UserValuation>.Filter.And(filter, Builders<UserValuation>.Filter.Gt(x => x.Timestamp, compareTime));
                
                var pipeline = new List<BsonDocument>
                {
                    new BsonDocument("$match", new BsonDocument
                    {
                        { "userId", UserId },
                        { "timestamp", new BsonDocument("$gt", compareTime) }
                    }),
                    new BsonDocument("$addFields", new BsonDocument("day", new BsonDocument("$dayOfYear", "$timestamp"))),
                    new BsonDocument("$sort", new BsonDocument("timestamp", 1)),
                    new BsonDocument("$group", new BsonDocument
                    {
                        { "_id", "$day" },
                        { "first", new BsonDocument("$first", "$$ROOT") }
                    }),
                    new BsonDocument("$replaceRoot", new BsonDocument("newRoot", "$first"))
                };

                data = await _dbContext.UserValuations.Aggregate<UserValuation>(pipeline).ToListAsync();
            }
            else if (period == "1m")
            {
                // One month, return one point per 3 days
                var compareTime = DateTime.UtcNow.AddDays(-31);
                var allData = await _dbContext.UserValuations
                    .Find(Builders<UserValuation>.Filter.And(
                        Builders<UserValuation>.Filter.Eq(x => x.UserId, UserId),
                        Builders<UserValuation>.Filter.Gt(x => x.Timestamp, compareTime)
                    ))
                    .Sort(Builders<UserValuation>.Sort.Ascending(x => x.Timestamp))
                    .ToListAsync();

                data = new List<UserValuation>();
                for(var i = allData.Count - 1; i >= 0; i -= 3)                
                    data.Add(allData[i]);                
            }
            else
            {
                // One year, return one point per month
                var compareTime = DateTime.UtcNow.AddDays(-366);
                var allData = await _dbContext.UserValuations
                    .Find(Builders<UserValuation>.Filter.And(
                        Builders<UserValuation>.Filter.Eq(x => x.UserId, UserId),
                        Builders<UserValuation>.Filter.Gt(x => x.Timestamp, compareTime)
                    ))
                    .Sort(Builders<UserValuation>.Sort.Ascending(x => x.Timestamp))
                    .ToListAsync();

                var lastMonth = -1;
                data = new List<UserValuation>();
                foreach(var item in allData)
                {
                    if (item.Timestamp.Month != lastMonth)
                    {
                        data.Add(item);
                        lastMonth = item.Timestamp.Month;
                    }
                }
            }

            return ApiResult<IEnumerable<ApiUserHistoryItem>>.Ok(data.OrderBy(x => x.Timestamp).Select(x => new ApiUserHistoryItem
            {
                Value = x.Value,
                Timestamp = new DateTime(x.Timestamp.Year, x.Timestamp.Month, x.Timestamp.Day)
            }));
        }

        [HttpGet("assets")]
        public async Task<ApiResult<IEnumerable<ApiAssetBalance>>> GetUserAssetsAsync(int amount = 10)
        {
            var exchangeData = await _dbContext.UserTotalExchangeAssetBalances(int.Parse(UserId));
            var externalData = await _dbContext.UserExternalBalances(int.Parse(UserId));
            
            var dataExchange = exchangeData.OrderByDescending(x => x.UsdValue).Take(amount).ToList();
            var dataExternal = externalData.OrderByDescending(x => x.UsdValue).Take(amount).ToList();

            var result = new List<ApiAssetBalance>();
            foreach (var item in dataExchange)
                result.Add(new ApiAssetBalance { Name = item.Asset, Value = item.UsdValue, Price = Math.Round(item.UsdValue / item.Total, 8) });

            foreach(var item in dataExternal)
            {
                var existing = result.SingleOrDefault(x => x.Name == item.Asset);
                if (existing == null)
                {
                    existing = new ApiAssetBalance { Name = item.Asset, Price = Math.Round(item.UsdValue / item.Total, 8) };
                    result.Add(existing);
                }

                existing.Value += item.UsdValue;
            }

            return ApiResult<IEnumerable<ApiAssetBalance>>.Ok(result.OrderByDescending(x => x.Value).Take(amount));
        }

        [HttpPost("external")]
        public async Task<ApiResult> UpdateExternalBalanceAsync([FromBody] UpdateExternalBalanceRequest request)
        {
            var filter = Builders<UserExternalBalance>.Filter.And(
                Builders<UserExternalBalance>.Filter.Eq(x => x.UserId, UserId),
                Builders<UserExternalBalance>.Filter.Eq(x => x.Asset, request.Asset)
            );
            
            var existingBalance = await _dbContext.UserExternalBalances.Find(filter).FirstOrDefaultAsync();
            if (existingBalance == null)
            {
                existingBalance = new UserExternalBalance()
                {
                    Id = $"{UserId}-{request.Asset}",
                    Asset = request.Asset,
                    UserId = UserId,
                    Total = request.Total
                };
                await _dbContext.UserExternalBalances.InsertOneAsync(existingBalance);
            }
            else
            {
                var update = Builders<UserExternalBalance>.Update.Set(x => x.Total, request.Total);
                await _dbContext.UserExternalBalances.UpdateOneAsync(filter, update);
            }

            return ApiResult.Ok();
        }

        [HttpDelete("external/{id}")]
        public async Task<ApiResult> DeleteExternalBalanceAsync(string id)
        {
            var filter = Builders<UserExternalBalance>.Filter.And(
                Builders<UserExternalBalance>.Filter.Eq(x => x.UserId, UserId),
                Builders<UserExternalBalance>.Filter.Eq(x => x.Id, id)
            );
            
            await _dbContext.UserExternalBalances.DeleteOneAsync(filter);
            return ApiResult.Ok();
        }

        [HttpPost("update")]
        public async Task<ApiResult> UpdateAsync(string? exchange)
        {
            if (!await CheckUserUpdateTopicAsync(UserUpdateType.Balances))
                return ApiResult.Ok();

            var apiKeyFilter = Builders<UserApiKey>.Filter.And(
                Builders<UserApiKey>.Filter.Eq(x => x.UserId, UserId),
                Builders<UserApiKey>.Filter.Eq(x => x.Invalid, false)
            );
            
            if (!string.IsNullOrEmpty(exchange))
            {
                apiKeyFilter = Builders<UserApiKey>.Filter.And(apiKeyFilter, Builders<UserApiKey>.Filter.Eq(x => x.Exchange, exchange));
            }
            
            var apiKeys = await _dbContext.UserApiKeys.Find(apiKeyFilter).ToListAsync();
            if (!string.IsNullOrEmpty(exchange) && !apiKeys.Any())
                return ApiResult.Error(ApiErrors.NoApiKeyConfigured);

            var environments = apiKeys.ToDictionary(x => x.Exchange, x => (string?)x.Environment);
            var credentials = apiKeys.ToDictionary(x => x.Exchange, x => new ApiCredentials(x.Key, x.Secret, x.Pass));
            var client = _clientProvider.GetRestClient(UserId.ToString(), new ExchangeCredentials(credentials), environments);

            var balanceResults = await client.GetBalancesAsync(new GetBalancesRequest(TradingMode.Spot), apiKeys.Select(x => x.Exchange));

            var dbBalances = new List<UserBalance>();
            foreach (var result in balanceResults.Where(x => x.Success))
            {
                dbBalances.AddRange(result.Data.Select(x => new UserBalance
                {
                    Id = $"{UserId}-{result.Exchange}-{x.Asset}",
                    Exchange = result.Exchange,
                    Asset = x.Asset,
                    Available = x.Available,
                    Total = x.Total,
                    UserId = UserId
                }));
            }

            await _dbContext.BulkInsertOrUpdateAsync(dbBalances);
            return ApiResult.Ok();
        }
    }
}
