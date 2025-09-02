using CryptoManager.Net.Models.Response;

namespace CryptoManager.Net.UI.Services.Stream
{

    internal class WebsocketSubscription
    {
        public SubscriptionTopic Topic { get; set; }
        public string Id { get; set; } = string.Empty;
        public string SymbolId { get; set; } = string.Empty;
        public bool Confirmed { get; set; }
        public Action<SubscriptionEvent> StatusCallback { get; set; }

        public WebsocketSubscription(string id, SubscriptionTopic topic, string symbolId, Action<SubscriptionEvent> statusCallback)
        {
            Id = id;
            Topic = topic;
            SymbolId = symbolId;
            StatusCallback = statusCallback;
        }
    }

    internal class TickerWebsocketSubscription : WebsocketSubscription
    {
        public Action<ApiTicker> DataCallback { get; set; }

        public TickerWebsocketSubscription(string id, string symbolId, Action<ApiTicker> callback, Action<SubscriptionEvent> statusCallback) : base(id, SubscriptionTopic.Ticker, symbolId, statusCallback)
        {
            DataCallback = callback;
        }
    }

    internal class TradeWebsocketSubscription : WebsocketSubscription
    {
        public Action<ApiTrade[]> DataCallback { get; set; }

        public TradeWebsocketSubscription(string id, string symbolId, Action<ApiTrade[]> callback, Action<SubscriptionEvent> statusCallback) : base(id, SubscriptionTopic.Trade, symbolId, statusCallback)
        {
            DataCallback = callback;
        }
    }

    internal class OrderBookWebsocketSubscription : WebsocketSubscription
    {
        public Action<ApiBook> DataCallback { get; set; }

        public OrderBookWebsocketSubscription(string id, string symbolId, Action<ApiBook> callback, Action<SubscriptionEvent> statusCallback) : base(id, SubscriptionTopic.OrderBook, symbolId, statusCallback)
        {
            DataCallback = callback;
        }
    }
}
