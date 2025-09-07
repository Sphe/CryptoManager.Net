using CryptoExchange.Net.SharedApis;
using CryptoManager.Net.Data;

namespace CryptoManager.Net.Subscriptions.KLine
{
    internal class ConnectionSubscription
    {
        public string SymbolId { get; set; }
        public string Interval { get; set; }
        public Action<SubscriptionEvent> StatusCallback { get; set; }
        public Action<ExchangeEvent<SharedKline>> DataCallback { get; set; }

        public ConnectionSubscription(string symbolId, string interval, Action<SubscriptionEvent> statusCallback, Action<ExchangeEvent<SharedKline>> dataCallback)
        {
            SymbolId = symbolId;
            Interval = interval;
            StatusCallback = statusCallback;
            DataCallback = dataCallback;
        }
    }
}
