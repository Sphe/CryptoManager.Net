namespace CryptoManager.Net.Database.Projections
{
    public class ExternalBalanceValue
    {
        public string Id { get; set; } = string.Empty;
        public string Asset { get; set; } = string.Empty;
        public decimal Total { get; set; }
        public decimal UsdValue { get; set; }
    }
}
