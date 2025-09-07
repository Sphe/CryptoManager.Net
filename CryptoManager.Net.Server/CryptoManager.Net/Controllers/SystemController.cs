using CryptoClients.Net.Interfaces;
using CryptoManager.Net.Caching;
using CryptoManager.Net.Database;
using CryptoManager.Net.Models.Response;
using CryptoManager.Net.Websockets;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using MongoDB.Bson;

namespace CryptoManager.Net.Controllers;

[Route("[controller]")]
[ResponseCache(Duration = 10, VaryByQueryKeys = ["*"])]
public class SystemController : ApiController
{
    private readonly ILogger _logger;
    private readonly IExchangeSocketClient _socketClient;
    private readonly WebsocketManager _websocketManager;

    public SystemController(
        ILogger<SystemController> logger,
        MongoTrackerContext context,
        IExchangeSocketClient socketClient,
        WebsocketManager websocketManager) : base(context)
    {
        _logger = logger;
        _socketClient = socketClient;
        _websocketManager = websocketManager;
    }

    [HttpGet()]
    [AllowAnonymous]
    [ServerCache(Duration = 10)]
    public async Task<ApiResult<ApiSystemStatus>> GetSystemStatsAsync()
    {
        // Get distinct exchanges using MongoDB aggregation
        var exchangesPipeline = new[]
        {
            new BsonDocument("$group", new BsonDocument("_id", "$Exchange")),
            new BsonDocument("$count", "count")
        };
        var exchangesResult = await _dbContext.Symbols.Aggregate<BsonDocument>(exchangesPipeline).FirstOrDefaultAsync();
        var exchangesCount = exchangesResult?["count"]?.AsInt32 ?? 0;

        // Get distinct assets using MongoDB aggregation
        var assetsPipeline = new[]
        {
            new BsonDocument("$group", new BsonDocument("_id", "$BaseAsset")),
            new BsonDocument("$count", "count")
        };
        var assetsResult = await _dbContext.Symbols.Aggregate<BsonDocument>(assetsPipeline).FirstOrDefaultAsync();
        var assetsCount = assetsResult?["count"]?.AsInt32 ?? 0;

        return ApiResult<ApiSystemStatus>.Ok(new ApiSystemStatus
        {
            IncomingKbps = _socketClient.IncomingKbps,

            Exchanges = exchangesCount,
            Symbols = (int)await _dbContext.Symbols.CountDocumentsAsync(_ => true),
            Assets = assetsCount,

            WebsocketConnections = _websocketManager.ConnectionCount,
            UserSubscriptions = _websocketManager.UserConnectionCount,
            TickerConnections = _websocketManager.TickerConnectionCount,
            TickerSubscriptions = _websocketManager.TickerSubscriptionCount,
            TradeConnections = _websocketManager.TradeConnectionCount,
            TradeSubscriptions = _websocketManager.TradeSubscriptionCount,
            OrderBookConnections = _websocketManager.OrderBookConnectionCount,
            OrderBookSubscriptions = _websocketManager.OrderBookSubscriptionCount,
        });
    }
}
