using CryptoExchange.Net.Interfaces;

namespace CryptoManager.Net.Subscriptions.Trades
{
    internal class OrderBookUpdateSubscription
    {
        public int SubscriptionCount { get; set; }
        public Task UpdateTask { get; set; }
        public ISymbolOrderBook UpdateSubscription { get; set; }

        public OrderBookUpdateSubscription(int subscriptionCount, Task updateTask, ISymbolOrderBook updateSubscription)
        {
            SubscriptionCount = subscriptionCount;
            UpdateTask = updateTask;
            UpdateSubscription = updateSubscription;
        }
    }
}
