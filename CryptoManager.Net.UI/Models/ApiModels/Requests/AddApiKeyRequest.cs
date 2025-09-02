namespace CryptoManager.Net.UI.Models.ApiModels.Requests
{
    public class AddApiKeyRequest
    {
        public string Exchange { get; set; } = string.Empty;
        public string Environment { get; set; } = string.Empty;
        public string Key { get; set; } = string.Empty;
        public string Secret { get; set; } = string.Empty;
        public string? Pass { get; set; }
    }
}
