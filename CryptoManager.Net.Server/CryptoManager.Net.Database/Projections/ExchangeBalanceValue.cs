namespace CryptoManager.Net.Database.Projections
{
    public class ExchangeBalanceValue
    {
        public string Exchange { get; set; } = string.Empty;
        public string Asset { get; set; } = string.Empty;
        public decimal Available { get; set; }
        public decimal Total { get; set; }
        public decimal UsdValue { get; set; }
    }
}
