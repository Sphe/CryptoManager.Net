using CryptoExchange.Net.SharedApis;

namespace CryptoManager.Net.UI.Models.ApiModels.Response
{
    public class ApiUserTrade
    {
        public string Id { get; set; } = string.Empty;
        public string Exchange { get; set; } = string.Empty;
        public string SymbolId { get; set; } = string.Empty;
        public decimal? Price { get; set; }
        public decimal? Quantity { get; set; }
        public string? FeeAsset { get; set; }
        public decimal? Fee { get; set; }
        public SharedRole? Role { get; set; }
        public SharedOrderSide? OrderSide { get; set; }
        public DateTime? CreateTime { get; set; }
    }
}
