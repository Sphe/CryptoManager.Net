using CryptoManager.Net.Models;
using CryptoManager.Net.Publish;
using System.Threading.Channels;

namespace CryptoManager.Net
{
    public static class ChannelRegistrations
    {
        private static BoundedChannelOptions GetChannelOptions(int maxCap) => new BoundedChannelOptions(maxCap) { SingleWriter = true, SingleReader = true, FullMode = BoundedChannelFullMode.DropOldest, AllowSynchronousContinuations = false };

        public static Channel<PublishItem<Symbol>> SymbolChannel { get; } = Channel.CreateBounded<PublishItem<Symbol>>(GetChannelOptions(100));
        public static Channel<PublishItem<Ticker>> TickerChannel { get; } = Channel.CreateBounded<PublishItem<Ticker>>(GetChannelOptions(100));
        public static Channel<PublishItem<FiatPrice>> FiatPriceChannel { get; } = Channel.CreateBounded<PublishItem<FiatPrice>>(GetChannelOptions(100));
        public static Channel<PublishItem<PendingAssetCalculation>> AssetCalculationChannel { get; } = Channel.CreateBounded<PublishItem<PendingAssetCalculation>>(GetChannelOptions(100));
    }
}
