namespace CryptoManager.Net.UI.Models.ApiModels.Response
{
    public class ApiBalance
    {
        public string Id { get; set; } = string.Empty;
        public string Asset { get; set; } = string.Empty;
        public string? Exchange { get; set; }
        public decimal? Available { get; set; }
        public decimal Total { get; set; }
        public decimal? UsdValue { get; set; }
    }
}
