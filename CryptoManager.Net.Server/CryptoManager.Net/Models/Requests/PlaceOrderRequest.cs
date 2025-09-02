using CryptoExchange.Net.SharedApis;

namespace CryptoManager.Net.Models.Requests
{
    public class PlaceOrderRequest
    {
        public string SymbolId { get; set; } = string.Empty;
        public SharedOrderSide OrderSide { get; set; }
        public SharedOrderType OrderType { get; set; }
        public SharedTimeInForce? TimeInForce { get; set; }
        public decimal? LimitPrice { get; set; }
        public decimal? BaseQuantity { get; set; }
        public decimal? QuoteQuantity { get; set; }
    }
}
