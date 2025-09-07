using CryptoManager.Net.Websockets;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CryptoManager.Net.Controllers;

/// <summary>
/// Controller for WebSocket connections
/// </summary>
[ApiController]
public class WebsocketController : ControllerBase
{
    private readonly ILogger _logger;
    private readonly WebsocketManager _websocketManager;

    public WebsocketController(ILogger<WebsocketController> logger, WebsocketManager manager)
    {
        _logger = logger;
        _websocketManager = manager;
    }

    /// <summary>
    /// Establishes a WebSocket connection for real-time data streaming
    /// </summary>
    /// <returns>WebSocket connection</returns>
    /// <response code="101">WebSocket connection established</response>
    /// <response code="400">Invalid WebSocket request</response>
    [HttpGet]
    [Route("/ws")]
    [AllowAnonymous]
    public async Task ConnectAsync()
    {
        if (HttpContext.WebSockets.IsWebSocketRequest)
        {
            using var webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();
            var socketFinishedTcs = new TaskCompletionSource();
            _websocketManager.AddConnection(webSocket, socketFinishedTcs);
            await socketFinishedTcs.Task;
        }
        else
        {
            HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
        }
    }
}
