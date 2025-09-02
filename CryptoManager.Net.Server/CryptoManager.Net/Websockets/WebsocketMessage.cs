using System.Text.Json.Serialization;

namespace CryptoManager.Net.Websockets
{
    internal class WebsocketMessage
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;
        [JsonPropertyName("action")]
        public MessageAction? Action { get; set; }
        [JsonPropertyName("topic")]
        public SubscriptionTopic? Topic { get; set; }
        [JsonPropertyName("symbol")]
        public string? SymbolId { get; set; }
        [JsonPropertyName("jwt")]
        public string? Jwt { get; set; }
    }
}
