using System.Text.Json.Serialization;

namespace CryptoManager.Net.Websockets
{
    internal class WebsocketDataUpdate<T>
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("topic"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public string? Topic { get; set; }
        [JsonPropertyName("data")]
        public T Data { get; set; } = default!;
    }
}
