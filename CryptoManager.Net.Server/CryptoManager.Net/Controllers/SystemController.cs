using CryptoClients.Net.Interfaces;
using CryptoManager.Net.Caching;
using CryptoManager.Net.Database;
using CryptoManager.Net.Models.Response;
using CryptoManager.Net.Websockets;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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
        TrackerContext context,
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
        return ApiResult<ApiSystemStatus>.Ok(new ApiSystemStatus
        {
            IncomingKbps = _socketClient.IncomingKbps,

            Exchanges = await _dbContext.Symbols.Select(x => x.Exchange).Distinct().CountAsync(),
            Symbols = await _dbContext.Symbols.CountAsync(),
            Assets = await _dbContext.Symbols.Select(x => x.BaseAsset).Distinct().CountAsync(),

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
