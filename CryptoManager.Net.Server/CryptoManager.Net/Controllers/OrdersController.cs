using CryptoClients.Net.Interfaces;
using CryptoClients.Net.Models;
using CryptoExchange.Net;
using CryptoExchange.Net.Authentication;
using CryptoExchange.Net.Objects.Errors;
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
    [ResponseCache(Duration = 3, Location = ResponseCacheLocation.Client, VaryByQueryKeys = ["*"])]
    [ServerCache(Duration = 3)]
    public class OrdersController : ApiController
    {
        private readonly ILogger _logger;
        private readonly IExchangeUserClientProvider _clientProvider;

        public OrdersController(ILogger<BalancesController> logger, IExchangeUserClientProvider clientProvider, MongoTrackerContext dbContext) : base(dbContext)
        {
            _logger = logger;
            _clientProvider = clientProvider;
        }

        [HttpGet("open")]
        public async Task<ApiResultPaged<IEnumerable<ApiOrder>>> ListOpenAsync(
            string? exchange,
            string? baseAsset,
            string? quoteAsset,
            string? orderBy = null,
            OrderDirection? orderDirection = null,
            int page = 1,
            int pageSize = 20)
        {
            var filterBuilder = Builders<UserOrder>.Filter;
            var filter = filterBuilder.And(
                filterBuilder.Eq(x => x.UserId, UserId),
                filterBuilder.Eq(x => x.Status, SharedOrderStatus.Open)
            );

            if (!string.IsNullOrEmpty(exchange))
                filter = filterBuilder.And(filter, filterBuilder.Eq(x => x.Exchange, exchange));

            if (!string.IsNullOrEmpty(baseAsset) && !string.IsNullOrEmpty(quoteAsset))
                filter = filterBuilder.And(filter, filterBuilder.Regex(x => x.SymbolId, new BsonRegularExpression($"{baseAsset}-{quoteAsset}$")));
            else if (!string.IsNullOrEmpty(baseAsset))
                filter = filterBuilder.And(filter, filterBuilder.Regex(x => x.SymbolId, new BsonRegularExpression($"-{baseAsset}-")));
            else if (!string.IsNullOrEmpty(quoteAsset))
                filter = filterBuilder.And(filter, filterBuilder.Regex(x => x.SymbolId, new BsonRegularExpression($"-{quoteAsset}$")));

            if (string.IsNullOrEmpty(orderBy))
                orderBy = nameof(ApiOrder.CreateTime);

            var sortBuilder = Builders<UserOrder>.Sort;
            var sort = orderBy switch
            {
                nameof(ApiOrder.CreateTime) => orderDirection == OrderDirection.Ascending ? sortBuilder.Ascending(x => x.CreateTime) : sortBuilder.Descending(x => x.CreateTime),
                nameof(ApiOrder.OrderPrice) => orderDirection == OrderDirection.Ascending ? sortBuilder.Ascending(x => x.OrderPrice) : sortBuilder.Descending(x => x.OrderPrice),
                nameof(ApiOrder.AveragePrice) => orderDirection == OrderDirection.Ascending ? sortBuilder.Ascending(x => x.AveragePrice) : sortBuilder.Descending(x => x.AveragePrice),
                nameof(ApiOrder.OrderQuantityBase) => orderDirection == OrderDirection.Ascending ? sortBuilder.Ascending(x => x.OrderQuantityBase) : sortBuilder.Descending(x => x.OrderQuantityBase),
                nameof(ApiOrder.OrderQuantityQuote) => orderDirection == OrderDirection.Ascending ? sortBuilder.Ascending(x => x.OrderQuantityQuote) : sortBuilder.Descending(x => x.OrderQuantityQuote),
                _ => throw new ArgumentException(),
            };

            var total = await _dbContext.UserOrders.CountDocumentsAsync(filter);
            var pageData = await _dbContext.UserOrders.Find(filter).Sort(sort).Skip((page - 1) * pageSize).Limit(pageSize).ToListAsync();

            return ApiResultPaged<IEnumerable<ApiOrder>>.Ok(page, pageSize, (int)total, pageData.Select(x => new ApiOrder
            {
                Exchange = x.Exchange,
                AveragePrice = x.AveragePrice,
                SymbolId = x.SymbolId,
                CreateTime = x.CreateTime,
                Id = x.Id,
                OrderPrice = x.OrderPrice,
                OrderQuantityBase = x.OrderQuantityBase,
                OrderQuantityQuote = x.OrderQuantityQuote,
                OrderSide = x.OrderSide,
                OrderType = x.OrderType,
                QuantityFilledBase = x.QuantityFilledBase,
                QuantityFilledQuote = x.QuantityFilledQuote,
                Status = x.Status
            }));
        }

        [HttpGet("closed")]
        public async Task<ApiResultPaged<IEnumerable<ApiOrder>>> ListClosedAsync(
            string? exchange,
            string baseAsset,
            string quoteAsset,
            string? orderBy = null,
            OrderDirection? orderDirection = null, 
            int page = 1,
            int pageSize = 20)
        {
            var partSymbolId = $"-{baseAsset}-{quoteAsset}";
            var filterBuilder = Builders<UserOrder>.Filter;
            var filter = filterBuilder.And(
                filterBuilder.Eq(x => x.UserId, UserId),
                filterBuilder.Regex(x => x.SymbolId, new BsonRegularExpression($"{partSymbolId}$")),
                filterBuilder.Ne(x => x.Status, SharedOrderStatus.Open)
            );

            if (!string.IsNullOrEmpty(exchange))
                filter = filterBuilder.And(filter, filterBuilder.Eq(x => x.Exchange, exchange));

            if (string.IsNullOrEmpty(orderBy))
                orderBy = nameof(ApiOrder.CreateTime);

            var sortBuilder = Builders<UserOrder>.Sort;
            var sort = orderBy switch
            {
                nameof(ApiOrder.CreateTime) => orderDirection == OrderDirection.Ascending ? sortBuilder.Ascending(x => x.CreateTime) : sortBuilder.Descending(x => x.CreateTime),
                nameof(ApiOrder.OrderPrice) => orderDirection == OrderDirection.Ascending ? sortBuilder.Ascending(x => x.OrderPrice) : sortBuilder.Descending(x => x.OrderPrice),
                nameof(ApiOrder.AveragePrice) => orderDirection == OrderDirection.Ascending ? sortBuilder.Ascending(x => x.AveragePrice) : sortBuilder.Descending(x => x.AveragePrice),
                nameof(ApiOrder.OrderQuantityBase) => orderDirection == OrderDirection.Ascending ? sortBuilder.Ascending(x => x.OrderQuantityBase) : sortBuilder.Descending(x => x.OrderQuantityBase),
                nameof(ApiOrder.OrderQuantityQuote) => orderDirection == OrderDirection.Ascending ? sortBuilder.Ascending(x => x.OrderQuantityQuote) : sortBuilder.Descending(x => x.OrderQuantityQuote),
                _ => throw new ArgumentException(),
            };

            var total = await _dbContext.UserOrders.CountDocumentsAsync(filter);
            var pageData = await _dbContext.UserOrders.Find(filter).Sort(sort).Skip((page - 1) * pageSize).Limit(pageSize).ToListAsync();

            return ApiResultPaged<IEnumerable<ApiOrder>>.Ok(page, pageSize, (int)total, pageData.Select(x => new ApiOrder
            {
                Exchange = x.Exchange,
                AveragePrice = x.AveragePrice,
                SymbolId = x.SymbolId,
                CreateTime = x.CreateTime,
                Id = x.Id,
                OrderPrice = x.OrderPrice,
                OrderQuantityBase = x.OrderQuantityBase,
                OrderQuantityQuote = x.OrderQuantityQuote,
                OrderSide = x.OrderSide,
                OrderType = x.OrderType,
                QuantityFilledBase = x.QuantityFilledBase,
                QuantityFilledQuote = x.QuantityFilledQuote,
                Status = x.Status
            }));
        }

        [HttpPost]
        public async Task<ApiResult> PlaceOrderAsync([FromBody] PlaceOrderRequest request)
        {
            var symbolData = request.SymbolId.Split("-");
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
            var client = _clientProvider.GetRestClient(UserId.ToString()!, new ExchangeCredentials(credentials), environments);

            var price = request.LimitPrice;
            var orderClient = client.GetSpotOrderClient(symbolData[0]);
            if (price == null && orderClient!.PlaceSpotOrderOptions.RequiredOptionalParameters.Any(x => x.Name == nameof(PlaceSpotOrderRequest.Price)))
            {
                // Some exchanges require a price to be sent even for market orders so they can calculate a slippage and simulate a market order even 
                // if the exchange doesn't directly support it
                var tickerFilter = Builders<ExchangeSymbol>.Filter.Eq(x => x.Id, request.SymbolId);
                var ticker = await _dbContext.Symbols.Find(tickerFilter).FirstOrDefaultAsync();
                price = ticker?.LastPrice?.Normalize();
            }

            var sharedSymbol = new SharedSymbol(TradingMode.Spot, symbolData[1], symbolData[2]);
            var result = await client.PlaceSpotOrderAsync(symbolData[0],
                new PlaceSpotOrderRequest(
                    sharedSymbol,
                    request.OrderSide,
                    request.OrderType,
                    request.QuoteQuantity > 0 ? SharedQuantity.Quote(request.QuoteQuantity.Value) : SharedQuantity.Base(request.BaseQuantity!.Value),
                    price,
                    request.OrderType == SharedOrderType.LimitMaker ? null : request.TimeInForce));

            if (!result)
                return ApiResult.Error(result.Error!.ErrorType, result.Error.ErrorCode, result.Error.Message);

            return ApiResult.Ok();
        }

        [HttpDelete("{id}")]
        public async Task<ApiResult> CancelOrderAsync(string id)
        {
            var orderFilter = Builders<UserOrder>.Filter.And(
                Builders<UserOrder>.Filter.Eq(x => x.UserId, UserId),
                Builders<UserOrder>.Filter.Eq(x => x.Id, id)
            );
            var order = await _dbContext.UserOrders.Find(orderFilter).FirstOrDefaultAsync();
            if (order == null)
            {
                _logger.LogDebug("Order to cancel not found");
                return ApiResult.Error(ErrorType.UnknownOrder, null, "Not found");
            }

            var apiKeyFilter = Builders<UserApiKey>.Filter.And(
                Builders<UserApiKey>.Filter.Eq(x => x.UserId, UserId),
                Builders<UserApiKey>.Filter.Eq(x => x.Invalid, false),
                Builders<UserApiKey>.Filter.Eq(x => x.Exchange, order.Exchange)
            );
            var apiKeys = await _dbContext.UserApiKeys.Find(apiKeyFilter).ToListAsync();

            var environments = apiKeys.ToDictionary(x => x.Exchange, x => (string?)x.Environment);
            var credentials = apiKeys.ToDictionary(x => x.Exchange, x => new ApiCredentials(x.Key, x.Secret, x.Pass));
            var client = _clientProvider.GetRestClient(UserId.ToString()!, new ExchangeCredentials(credentials), environments);

            var symbolData = order.SymbolId.Split("-");
            var sharedSymbol = new SharedSymbol(TradingMode.Spot, symbolData[1], symbolData[2]);
            var result = await client.CancelSpotOrderAsync(symbolData[0], new CancelOrderRequest(sharedSymbol, order.OrderId));
            if (!result)
                return ApiResult.Error(result.Error!.ErrorType, result.Error!.ErrorCode, result.Error.Message);

            return ApiResult.Ok();
        }

        [HttpGet("{id}")]
        public async Task<ApiResult<ApiOrder>> GetOrderAsync(string id)
        {
            var orderFilter = Builders<UserOrder>.Filter.And(
                Builders<UserOrder>.Filter.Eq(x => x.UserId, UserId),
                Builders<UserOrder>.Filter.Eq(x => x.Id, id)
            );
            var order = await _dbContext.UserOrders.Find(orderFilter).FirstOrDefaultAsync();
            if (order == null)
                return ApiResult<ApiOrder>.Error(ErrorType.UnknownOrder, null, "Order not found");

            return ApiResult<ApiOrder>.Ok(new ApiOrder
            {
                Exchange = order.Exchange,
                AveragePrice = order.AveragePrice,
                SymbolId = order.SymbolId,
                CreateTime = order.CreateTime,
                Id = order.Id,
                OrderPrice = order.OrderPrice,
                OrderQuantityBase = order.OrderQuantityBase,
                OrderQuantityQuote = order.OrderQuantityQuote,
                OrderSide = order.OrderSide,
                OrderType = order.OrderType,
                QuantityFilledBase = order.QuantityFilledBase,
                QuantityFilledQuote = order.QuantityFilledQuote,
                Status = order.Status
            });
        }

        [HttpPost("update/open")]
        public async Task<ApiResult> UpdateAsync(string? symbolId = null)
        {
            if (!await CheckUserUpdateTopicAsync(UserUpdateType.OpenOrders))
                return ApiResult.Ok();

            string[]? symbolData = null;
            if (!string.IsNullOrEmpty(symbolId))
                symbolData = symbolId.Split("-");

            var keyFilter = Builders<UserApiKey>.Filter.And(
                Builders<UserApiKey>.Filter.Eq(x => x.UserId, UserId),
                Builders<UserApiKey>.Filter.Eq(x => x.Invalid, false)
            );
            if (symbolData != null)
                keyFilter = Builders<UserApiKey>.Filter.And(keyFilter, Builders<UserApiKey>.Filter.Eq(x => x.Exchange, symbolData[0]));
            var apiKeys = await _dbContext.UserApiKeys.Find(keyFilter).ToListAsync();
            if (!apiKeys.Any())
                return ApiResult.Error(ApiErrors.NoApiKeyConfigured);

            var environments = apiKeys.ToDictionary(x => x.Exchange, x => (string?)x.Environment);
            var credentials = apiKeys.ToDictionary(x => x.Exchange, x => new ApiCredentials(x.Key, x.Secret, x.Pass));
            var client = _clientProvider.GetRestClient(UserId.ToString()!, new ExchangeCredentials(credentials), environments);

            ExchangeWebResult<SharedSpotOrder[]>[] orders;
            if (symbolData == null)
            {
                // Filter out any clients for which the symbol parameter is required since we don't have it
                var exchanges = apiKeys.Select(x => x.Exchange).ToList();
                var orderClients = client.GetSpotOrderClients().Where(x => exchanges.Contains(x.Exchange)).ToList();
                foreach (var symbolIdRequiredClient in orderClients.Where(x => x.GetOpenSpotOrdersOptions.RequiredOptionalParameters.Any(x => x.Name == nameof(GetOpenOrdersRequest.Symbol))))
                    exchanges.Remove(symbolIdRequiredClient.Exchange);

                orders = await client.GetSpotOpenOrdersAsync(new GetOpenOrdersRequest(), exchanges);
            }
            else
            {
                orders = await client.GetSpotOpenOrdersAsync(new GetOpenOrdersRequest(new SharedSymbol(TradingMode.Spot, symbolData[1], symbolData[2])), apiKeys.Select(x => x.Exchange));
            }

            var dbOrders = orders.Where(x => x.Success).SelectMany(x => x.Data.Select(y => ParseOrder(x.Exchange, y))).ToArray();
            await _dbContext.BulkInsertOrUpdateAsync(dbOrders);

            // For each exchange that returned a successful response, mark any locally-stored Open orders
            // that were NOT in the exchange response as Cancelled (they were cancelled/filled externally).
            var successfulExchanges = orders.Where(x => x.Success).Select(x => x.Exchange).ToHashSet();
            var returnedIds = dbOrders.Select(x => x.Id).ToHashSet();

            var staleOrderFilter = Builders<UserOrder>.Filter.And(
                Builders<UserOrder>.Filter.Eq(x => x.UserId, UserId),
                Builders<UserOrder>.Filter.Eq(x => x.Status, SharedOrderStatus.Open),
                Builders<UserOrder>.Filter.In(x => x.Exchange, successfulExchanges),
                Builders<UserOrder>.Filter.Nin(x => x.Id, returnedIds)
            );

            if (symbolData != null)
                staleOrderFilter = Builders<UserOrder>.Filter.And(staleOrderFilter,
                    Builders<UserOrder>.Filter.Regex(x => x.SymbolId, new BsonRegularExpression($"{symbolData[1]}-{symbolData[2]}$")));

            var cancelUpdate = Builders<UserOrder>.Update.Set(x => x.Status, SharedOrderStatus.Cancelled);
            await _dbContext.UserOrders.UpdateManyAsync(staleOrderFilter, cancelUpdate);

// TODO: Ignore errors here because they're probably because of symbol not existing. Should first check if the symbol exists on the exchange
            //var errors = orders.Where(x => !x).Select(x => new ApiError(x.Error!.ErrorType, x.Error.ErrorCode, x.Exchange + ": " +x.Error.Message));
            //if (errors.Any())
            //    return ApiResult.Error(errors);

            return ApiResult.Ok();
        }

        [HttpPost("update/closed")]
        public async Task<ApiResult> UpdateClosedAsync(string? exchange, string baseAsset, string quoteAsset)
        {
            if (!await CheckUserUpdateTopicAsync(UserUpdateType.ClosedOrders))
                return ApiResult.Ok();

            var apiKeyFilter = Builders<UserApiKey>.Filter.And(
                Builders<UserApiKey>.Filter.Eq(x => x.UserId, UserId),
                Builders<UserApiKey>.Filter.Eq(x => x.Invalid, false)
            );
            if (!string.IsNullOrEmpty(exchange))
                apiKeyFilter = Builders<UserApiKey>.Filter.And(apiKeyFilter, Builders<UserApiKey>.Filter.Eq(x => x.Exchange, exchange));
            var apiKeys = await _dbContext.UserApiKeys.Find(apiKeyFilter).ToListAsync();
            if (apiKeys == null)
                return ApiResult.Error(ApiErrors.NoApiKeyConfigured);

            var exchanges = apiKeys.Select(x => x.Exchange);
            var environments = apiKeys.ToDictionary(x => x.Exchange, x => x.Environment);
            var credentials = apiKeys.ToDictionary(x => x.Exchange, x => new ApiCredentials(x.Key, x.Secret, x.Pass));
            var client = _clientProvider.GetRestClient(UserId.ToString()!, new ExchangeCredentials(credentials), environments);

            var symbol = new SharedSymbol(TradingMode.Spot, baseAsset, quoteAsset);
            var closedOrders = await client.GetSpotClosedOrdersAsync(new GetClosedOrdersRequest(symbol), exchanges);

            var dbOrders = closedOrders.Where(x => x.Success).SelectMany(y => y.Data.Select(x => ParseOrder(y.Exchange, x)));
            // Want to update full?
            // Maybe keep a track of until what timestamp a users order history is fully synced so a new update can request up to that point?
            await _dbContext.BulkInsertOrUpdateAsync(dbOrders);

// TODO: Ignore errors here because they're probably because of symbol not existing. Should first check if the symbol exists on the exchange
            //var failed = closedOrders.FirstOrDefault(x => !x.Success);
            //if (failed != null)
            //    return ApiResult.Error(new ApiError(failed.Error!.ErrorType, failed.Error.ErrorCode, failed.Error.Message));

            return ApiResult.Ok();
        }

        private UserOrder ParseOrder(string exchange, SharedSpotOrder x)
        {
            return new UserOrder
            {
                Id = $"{UserId}-{exchange}-{x.SharedSymbol!.BaseAsset}-{x.SharedSymbol!.QuoteAsset}-{x.OrderId}",
                Exchange = exchange,
                AveragePrice = x.AveragePrice,
                SymbolId = $"{exchange}-{x.SharedSymbol!.BaseAsset}-{x.SharedSymbol!.QuoteAsset}",
                CreateTime = x.CreateTime ?? DateTime.UtcNow,
                UpdateTime = x.UpdateTime ?? DateTime.UtcNow,
                OrderId = x.OrderId,
                OrderPrice = x.OrderPrice,
                OrderQuantityBase = x.OrderQuantity?.QuantityInBaseAsset,
                OrderQuantityQuote = x.OrderQuantity?.QuantityInQuoteAsset,
                OrderSide = x.Side,
                OrderType = x.OrderType,
                QuantityFilledBase = x.QuantityFilled?.QuantityInBaseAsset,
                QuantityFilledQuote = x.QuantityFilled?.QuantityInQuoteAsset,
                Status = x.Status,
                UserId = UserId
            };
        }
    }
}
