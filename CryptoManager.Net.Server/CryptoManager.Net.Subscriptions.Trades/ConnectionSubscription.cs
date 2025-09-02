using CryptoExchange.Net.SharedApis;
using CryptoManager.Net.Data;

namespace CryptoManager.Net.Subscriptions.Trades
{
    internal class ConnectionSubscription
    {
        public string SymbolId { get; set; }
        public Action<SubscriptionEvent> StatusCallback { get; set; }
        public Action<ExchangeEvent<SharedTrade[]>> DataCallback { get; set; }

        public ConnectionSubscription(string symbolId, Action<SubscriptionEvent> statusCallback, Action<ExchangeEvent<SharedTrade[]>> dataCallback)
        {
            SymbolId = symbolId;
            StatusCallback = statusCallback;
            DataCallback = dataCallback;
        }

    }
}
