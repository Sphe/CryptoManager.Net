namespace CryptoManager.Net.Subscriptions.User
{

    public class UserExchangeAuthentication
    {
        public string Exchange { get; set; } = string.Empty;
        public string? Environment { get; set; }
        public string ApiKey { get; set; } = string.Empty;
        public string ApiSecret { get; set; } = string.Empty;
        public string? ApiPass { get; set; }
    }
}
