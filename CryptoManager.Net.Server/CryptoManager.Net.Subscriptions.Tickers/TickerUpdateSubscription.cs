using CryptoExchange.Net.Objects.Sockets;

namespace CryptoManager.Net.Subscriptions.Tickers
{
    internal class TickerUpdateSubscription
    {
        public int SubscriptionCount { get; set; }
        public UpdateSubscription UpdateSubscription { get; set; }

        public TickerUpdateSubscription(int subscriptionCount, UpdateSubscription updateSubscription)
        {
            SubscriptionCount = subscriptionCount;
            UpdateSubscription = updateSubscription;
        }

    }
}
