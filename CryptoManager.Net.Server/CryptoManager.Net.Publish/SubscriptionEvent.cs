namespace CryptoManager.Net.Data
{
    public class SubscriptionEvent
    {
        public string? Exchange { get; set; }
        public SubscriptionStatus Status { get; set; }

        public SubscriptionEvent(SubscriptionStatus status)
        {
            Status = status;
        }
    }

    public enum SubscriptionStatus
    {
        Interrupted,
        Restored,
        Failed
    }
}
