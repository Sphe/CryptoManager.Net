namespace CryptoManager.Net.ApiModels.Requests
{
    public class AddApiKeyRequest
    {
        public string Exchange { get; set; } = string.Empty;
        public string? Environment { get; set; }
        public string Key { get; set; } = string.Empty;
        public string Secret { get; set; } = string.Empty;
        public string? Pass { get; set; }
    }
}
