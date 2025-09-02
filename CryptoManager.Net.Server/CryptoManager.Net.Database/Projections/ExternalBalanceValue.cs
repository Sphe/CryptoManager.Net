using Microsoft.EntityFrameworkCore;

namespace CryptoManager.Net.Database.Projections
{
    public class ExternalBalanceValue
    {
        public string Id { get; set; } = string.Empty;
        public string Asset { get; set; } = string.Empty;
        [Precision(28, 8)]
        public decimal Total { get; set; }
        [Precision(28, 8)]
        public decimal UsdValue { get; set; }
    }
}
