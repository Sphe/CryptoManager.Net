using Microsoft.EntityFrameworkCore;

namespace CryptoManager.Net.Database.Projections
{
    public class ExchangeUserValue
    {
        public int UserId { get; set; }
        [Precision(28, 8)]
        public decimal UsdValue { get; set; }
    }
}
