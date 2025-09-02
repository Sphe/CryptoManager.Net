using System.Net.WebSockets;

namespace CryptoManager.Net.Websockets
{
    internal class WebsocketConnection
    {
        public string Id { get; set; } = string.Empty;
        public Task? ProcessTask { get; set; }
        public DateTime ConnectTime { get; set; }
        public WebSocket Connection { get; set; }
        public TaskCompletionSource TaskCompletionSource { get; set; }
        public int? UserId { get; set; }

        public WebsocketConnection(string id, WebSocket connection, TaskCompletionSource taskCompletionSource)
        {
            Id = id;
            ConnectTime = DateTime.UtcNow;
            Connection = connection;
            TaskCompletionSource = taskCompletionSource;
        }
    }
}
