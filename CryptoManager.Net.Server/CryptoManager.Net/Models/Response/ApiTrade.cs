using CryptoExchange.Net.SharedApis;

namespace CryptoManager.Net.Models.Response
{
    public class ApiTrade
    {
        public decimal Quantity { get; set; }
        public decimal Price { get; set; }
        public DateTime Timestamp { get; set; }
        public SharedOrderSide? Side { get; set; }
    }
}
