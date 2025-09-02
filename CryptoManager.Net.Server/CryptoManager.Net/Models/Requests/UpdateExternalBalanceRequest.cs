namespace CryptoManager.Net.Models.Requests
{
    public class UpdateExternalBalanceRequest
    {
        public string Asset { get; set; } = string.Empty;
        public decimal Total { get; set; }
    }
}
