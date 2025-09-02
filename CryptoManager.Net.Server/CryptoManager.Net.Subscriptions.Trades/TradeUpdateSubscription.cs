using CryptoExchange.Net.Objects.Sockets;

namespace CryptoManager.Net.Subscriptions.Trades
{
    internal class TradeUpdateSubscription
    {
        public int SubscriptionCount { get; set; }
        public UpdateSubscription UpdateSubscription { get; set; }

        public TradeUpdateSubscription(int subscriptionCount, UpdateSubscription updateSubscription)
        {
            SubscriptionCount = subscriptionCount;
            UpdateSubscription = updateSubscription;
        }
    }
}
