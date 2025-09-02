namespace CryptoManager.Net.UI.Services.Stream
{
    public class SubscriptionEvent
    {
        public string Exchange { get; set; } = string.Empty;
        public string Info { get; set; } = string.Empty;
        public SubscriptionStatus Status { get; set; }
    }

    public enum SubscriptionStatus
    {
        Interrupted,
        Restored
    }
}
