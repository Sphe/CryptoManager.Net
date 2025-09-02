using System.Text.Json.Serialization;

namespace CryptoManager.Net.Websockets
{
    internal class WebsocketResponse
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }
        [JsonPropertyName("success")]
        public bool Success { get; set; }
        [JsonPropertyName("error"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public string? Error { get; set; }
    }
}
