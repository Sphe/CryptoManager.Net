using CryptoExchange.Net.SharedApis;

namespace CryptoManager.Net.Models.Response
{
    public class ApiOrder
    {
        public string Id { get; set; } = string.Empty;
        public string Exchange { get; set; } = string.Empty;
        public string SymbolId { get; set; } = string.Empty;
        public SharedOrderSide OrderSide { get; set; }
        public SharedOrderType OrderType { get; set; }
        public SharedOrderStatus Status { get; set; }
        public decimal? OrderPrice { get; set; }
        public decimal? OrderQuantityBase { get; set; }
        public decimal? OrderQuantityQuote { get; set; }
        public decimal? QuantityFilledBase { get; set; }
        public decimal? QuantityFilledQuote { get; set; }
        public decimal? AveragePrice { get; set; }
        public DateTime? CreateTime { get; set; }
    }
}
