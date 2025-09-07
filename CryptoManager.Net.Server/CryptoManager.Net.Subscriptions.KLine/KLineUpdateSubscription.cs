using CryptoExchange.Net.Objects.Sockets;

namespace CryptoManager.Net.Subscriptions.KLine
{
    internal class KLineUpdateSubscription
    {
        public int SubscriptionCount { get; set; }
        public UpdateSubscription UpdateSubscription { get; set; }

        public KLineUpdateSubscription(int subscriptionCount, UpdateSubscription updateSubscription)
        {
            SubscriptionCount = subscriptionCount;
            UpdateSubscription = updateSubscription;
        }
    }
}
