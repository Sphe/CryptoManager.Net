using CryptoClients.Net.Interfaces;
using CryptoClients.Net.Models;
using CryptoExchange.Net.Authentication;
using CryptoExchange.Net.SharedApis;
using CryptoManager.Net.Caching;
using CryptoManager.Net.Database;
using CryptoManager.Net.Database.Models;
using CryptoManager.Net.Models.Requests;
using CryptoManager.Net.Models.Response;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using MongoDB.Bson;
using System.Linq.Expressions;

namespace CryptoManager.Net.Controllers
{
    [Route("[controller]")]
    public class TradesController : ApiController
    {
        private readonly ILogger _logger;
        private readonly IExchangeUserClientProvider _clientProvider;

        public TradesController(ILogger<TradesController> logger, IExchangeUserClientProvider clientProvider, MongoTrackerContext dbContext)
            : base(dbContext)
        {
            _logger = logger;
            _clientProvider = clientProvider;
        }

        [HttpGet]
        [ResponseCache(Duration = 3, Location = ResponseCacheLocation.Client, VaryByQueryKeys=["*"])]
        [ServerCache(Duration = 3)]
        public async Task<ApiResultPaged<IEnumerable<ApiUserTrade>>> ListAsync(
            string? symbolId = null, 
            string? orderId = null,
            string? orderBy = null,
            OrderDirection? orderDirection = null,
            int page = 1, 
            int pageSize = 20)
        {
            var filterBuilder = Builders<UserTrade>.Filter;
            var filter = filterBuilder.Eq(x => x.UserId, UserId);
            
            if (!string.IsNullOrEmpty(symbolId))
                filter = filterBuilder.And(filter, filterBuilder.Eq(x => x.SymbolId, symbolId));

            if (!string.IsNullOrEmpty(orderId))
                filter = filterBuilder.And(filter, filterBuilder.Eq(x => x.OrderId, orderId));

            if (string.IsNullOrEmpty(orderBy))
                orderBy = nameof(ApiOrder.CreateTime);

            var sortBuilder = Builders<UserTrade>.Sort;
            var sort = orderBy switch
            {
                nameof(UserTrade.CreateTime) => orderDirection == OrderDirection.Ascending ? sortBuilder.Ascending(x => x.CreateTime) : sortBuilder.Descending(x => x.CreateTime),
                _ => throw new ArgumentException(),
            };

            var total = await _dbContext.UserTrades.CountDocumentsAsync(filter);
            var pageData = await _dbContext.UserTrades.Find(filter).Sort(sort).Skip((page - 1) * pageSize).Limit(pageSize).ToListAsync();

            return ApiResultPaged<IEnumerable<ApiUserTrade>>.Ok(page, pageSize, (int)total, pageData.Select(x => new ApiUserTrade
            {
                Exchange = x.Exchange,
                SymbolId = x.SymbolId,
                CreateTime = x.CreateTime,
                OrderSide = x.Side,
                Id = x.Id,
                Fee = x.Fee,
                FeeAsset = x.FeeAsset,
                Price = x.Price,
                Quantity = x.Quantity,
                Role = x.Role
            }));
        }

        [HttpPost("update")]
        public async Task<ApiResult> UpdateAsync(string symbolId, string? orderId = null)
        {
            if (!await CheckUserUpdateTopicAsync(UserUpdateType.ClosedOrders))
                return ApiResult.Ok();

            var symbolData = symbolId.Split("-");
            var apiKeyFilter = Builders<UserApiKey>.Filter.And(
                Builders<UserApiKey>.Filter.Eq(x => x.UserId, UserId),
                Builders<UserApiKey>.Filter.Eq(x => x.Invalid, false),
                Builders<UserApiKey>.Filter.Eq(x => x.Exchange, symbolData[0])
            );
            var apiKey = await _dbContext.UserApiKeys.Find(apiKeyFilter).FirstOrDefaultAsync();
            if (apiKey == null)
                return ApiResult.Error(ApiErrors.NoApiKeyConfigured);

            var environments = new Dictionary<string, string?>() { { apiKey.Exchange, apiKey.Environment } };
            var credentials = new Dictionary<string, ApiCredentials>() { { apiKey.Exchange, new ApiCredentials(apiKey.Key, apiKey.Secret, apiKey.Pass) } };
            var client = _clientProvider.GetRestClient(UserId.ToString(), new ExchangeCredentials(credentials), environments);

            ExchangeWebResult<SharedUserTrade[]> userTrades;
            if (!string.IsNullOrEmpty(orderId))
            {
                var orderIdData = orderId.Split("-");
                userTrades = await client.GetSpotOrderTradesAsync(symbolData[0], new GetOrderTradesRequest(new SharedSymbol(TradingMode.Spot, symbolData[1], symbolData[2]), orderIdData[4]));
                
            }
            else
            {
                userTrades = await client.GetSpotUserTradesAsync(symbolData[0], new GetUserTradesRequest(new SharedSymbol(TradingMode.Spot, symbolData[1], symbolData[2])));
            }

            if (!userTrades)
                return ApiResult.Error(userTrades.Error!.ErrorType, userTrades.Error.ErrorCode, userTrades.Error.Message);

            var dbTrades = userTrades.Data.Select(x => new UserTrade
            {
                Id = $"{UserId}-{userTrades.Exchange}-{x.SharedSymbol!.BaseAsset}-{x.SharedSymbol!.QuoteAsset}-{x.Id}",
                UserId = UserId,
                OrderId = $"{UserId}-{userTrades.Exchange}-{x.SharedSymbol!.BaseAsset}-{x.SharedSymbol!.QuoteAsset}-{x.OrderId}",
                SymbolId = $"{userTrades.Exchange}-{x.SharedSymbol!.BaseAsset}-{x.SharedSymbol!.QuoteAsset}",
                CreateTime = x.Timestamp,
                Exchange = userTrades.Exchange,
                Fee = x.Fee,
                FeeAsset = x.FeeAsset,
                Price = x.Price,
                Quantity = x.Quantity,
                Role = x.Role,
                TradeId = x.Id,
                Side = x.Side
            });

            await _dbContext.BulkInsertOrUpdateAsync(dbTrades);
            return ApiResult.Ok();
        }

    }
}
