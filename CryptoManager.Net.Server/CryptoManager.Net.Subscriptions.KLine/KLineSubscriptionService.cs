using CryptoClients.Net.Interfaces;
using CryptoExchange.Net.Objects;
using CryptoExchange.Net.SharedApis;
using CryptoManager.Net.Data;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace CryptoManager.Net.Subscriptions.KLine
{
    public class KLineSubscriptionService
    {
        private readonly ILogger _logger;
        private readonly IExchangeSocketClient _socketClient;

        private readonly ConcurrentDictionary<string, SemaphoreSlim> _symbolSemaphores = new ConcurrentDictionary<string, SemaphoreSlim>();

        // ConnectionId => Subscriptions
        private ConcurrentDictionary<string, ConcurrentDictionary<string, ConnectionSubscription>> _connectionSubscriptions = new ConcurrentDictionary<string, ConcurrentDictionary<string, ConnectionSubscription>>();
        // TopicKey => UpdateSubscriptions
        private ConcurrentDictionary<string, KLineUpdateSubscription> _updateSubscriptions = new ConcurrentDictionary<string, KLineUpdateSubscription>();

        public int ConnectionCount => _connectionSubscriptions.Count(x => x.Value.Count > 0);
        public int SubscriptionCount => _connectionSubscriptions.Sum(x => x.Value.Count);

        public KLineSubscriptionService(
            ILogger<KLineSubscriptionService> logger,
            IExchangeSocketClient socketClient)
        {
            _logger = logger;
            _socketClient = socketClient;
        }

        public async Task<CallResult> SubscribeAsync(
            string connectionId,
            string symbolId,
            string interval,
            Action<ExchangeEvent<SharedKline>> handler,
            Action<SubscriptionEvent> statusHandler,
            CancellationToken ct)
        {
            var symbolData = symbolId.Split("-");
            var client = _socketClient.GetKlineClient(TradingMode.Spot, symbolData[0]);
            if (client == null)
                return new CallResult(ArgumentError.Invalid(symbolId, $"No KLine subscription client available for {symbolData[0]}"));

            var topicKey = $"{symbolId}-{interval}";

            // Could be improved if semaphore is released earlier and a new semaphore is acquired only for a specific symbol
            var semaphore = _symbolSemaphores.GetOrAdd(topicKey, x => new SemaphoreSlim(1, 1));
            await semaphore.WaitAsync();
            try
            {
                if (!_connectionSubscriptions.TryGetValue(connectionId, out var connectionSubs))
                {
                    connectionSubs = new ConcurrentDictionary<string, ConnectionSubscription>();
                    _connectionSubscriptions.TryAdd(connectionId, connectionSubs);
                }

                if (connectionSubs.TryGetValue(topicKey, out _))
                {
                    _logger.LogInformation("Connection already has a KLine subscription for {Symbol} with interval {Interval}", symbolId, interval);
                    return CallResult.SuccessResult;
                }

                connectionSubs.TryAdd(topicKey, new ConnectionSubscription(symbolId, interval, statusHandler, handler));

                if (_updateSubscriptions.TryGetValue(topicKey, out var existingSub))
                {
                    existingSub.SubscriptionCount++;
                    _logger.LogInformation("Adding callback to existing KLine subscription for {Symbol} with interval {Interval}, now {SubsOnSymbol}. {TotalSubs} KLine subscriptions for {DiffSubs} different symbols in total", symbolId, interval, existingSub.SubscriptionCount, _connectionSubscriptions.Sum(x => x.Value.Count), _updateSubscriptions.Count);
                    return CallResult.SuccessResult;
                }

                _logger.LogInformation("Subscribing to KLine updates for {Symbol} with interval {Interval}", symbolId, interval);
                var subscription = await client.SubscribeToKlineUpdatesAsync(new SubscribeKlineRequest(new SharedSymbol(TradingMode.Spot, symbolData[1], symbolData[2]), Enum.Parse<SharedKlineInterval>(interval)), update => ProcessUpdate(topicKey, update));
                if (!subscription)
                {
                    _logger.LogWarning("Failed to subscribe KLine for {Symbol} with interval {Interval}: {Error}", symbolId, interval, subscription.Error!.ToString());
                    return new CallResult(subscription.Error);
                }

                subscription.Data.ConnectionLost += () => ProcessConnectionLost(topicKey);
                subscription.Data.ConnectionRestored += x => ProcessConnectionRestored(topicKey);
                _updateSubscriptions.TryAdd(topicKey, new KLineUpdateSubscription(1, subscription.Data));
                _logger.LogInformation("Subscribed to KLine updates for {Symbol} with interval {Interval}, now {TotalSubs} KLine subscriptions for {DiffSubs} different symbols", symbolId, interval, _connectionSubscriptions.Sum(x => x.Value.Count), _updateSubscriptions.Count);

            }
            finally
            {
                semaphore.Release();
            }

            return CallResult.SuccessResult;
        }

        private void ProcessConnectionRestored(string topicKey)
        {
            var evnt = new SubscriptionEvent(SubscriptionStatus.Restored);
            foreach (var updateSubscription in _connectionSubscriptions.SelectMany(x => x.Value).Where(x => x.Key == topicKey))
                updateSubscription.Value.StatusCallback(evnt);
        }

        private void ProcessConnectionLost(string topicKey)
        {
            var evnt = new SubscriptionEvent(SubscriptionStatus.Interrupted);
            foreach (var updateSubscription in _connectionSubscriptions.SelectMany(x => x.Value).Where(x => x.Key == topicKey))
                updateSubscription.Value.StatusCallback(evnt);
        }

        private void ProcessUpdate(string topicKey, ExchangeEvent<SharedKline> update)
        {
            foreach (var updateSubscription in _connectionSubscriptions.SelectMany(x => x.Value).Where(x => x.Key == topicKey))
                updateSubscription.Value.DataCallback(update);
        }

        public async Task UnsubscribeAsync(string connectionId, string symbolId, string interval)
        {
            var topicKey = $"{symbolId}-{interval}";
            var semaphore = _symbolSemaphores[topicKey];
            await semaphore.WaitAsync();
            try
            {
                if (!_connectionSubscriptions.TryGetValue(connectionId, out var connectionSubscriptions))
                    return;

                if (!connectionSubscriptions.TryGetValue(topicKey, out var connectionSubscription))
                    return;

                connectionSubscriptions.Remove(topicKey, out _);

                if (!_updateSubscriptions.TryGetValue(topicKey, out var updateSubscription))
                    return;

                updateSubscription.SubscriptionCount -= 1;
                if (updateSubscription.SubscriptionCount > 0)
                {
                    _logger.LogInformation("KLine subscription removed for {Symbol} with interval {Interval}, now {SymbolSubs} subscription left on symbol. {TotalSubs} KLine subscriptions for {DiffSubs} different symbols", symbolId, interval, updateSubscription.SubscriptionCount, _connectionSubscriptions.Sum(x => x.Value.Count), _updateSubscriptions.Count);
                    return;
                }

                await updateSubscription.UpdateSubscription.CloseAsync();
                _updateSubscriptions.Remove(topicKey, out _);

                _logger.LogInformation("KLine subscription for {Symbol} with interval {Interval} closed, now {TotalSubs} KLine subscriptions for {DiffSubs} different symbols", symbolId, interval, _connectionSubscriptions.Sum(x => x.Value.Count), _updateSubscriptions.Count);
            }
            finally
            {
                semaphore.Release();
            }
        }

        public async Task UnsubscribeAllAsync(string connectionId)
        {
            if (!_connectionSubscriptions.TryGetValue(connectionId, out var subscriptions))
                return;

            foreach (var sub in subscriptions)
            {
                var parts = sub.Key.Split('-');
                var symbolId = string.Join("-", parts.Take(parts.Length - 1));
                var interval = parts.Last();
                await UnsubscribeAsync(connectionId, symbolId, interval);
            }

            _connectionSubscriptions.TryRemove(connectionId, out _);
        }
    }
}
