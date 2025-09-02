using CryptoManager.Net.Websockets;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CryptoManager.Net.Controllers;

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
