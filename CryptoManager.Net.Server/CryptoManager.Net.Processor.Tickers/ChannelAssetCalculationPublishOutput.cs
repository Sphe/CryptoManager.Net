using CryptoManager.Net.Models;
using CryptoManager.Net.Publish;
using System.Threading.Channels;

namespace CryptoManager.Net.Processor.Tickers
{
    public class ChannelAssetCalculationPublishOutput : IPublishOutput<PendingAssetCalculation>
    {
        private readonly ChannelWriter<PublishItem<PendingAssetCalculation>> _channelWriter;

        public ChannelAssetCalculationPublishOutput(ChannelWriter<PublishItem<PendingAssetCalculation>> channelWriter)
        {
            _channelWriter = channelWriter;
        }

        public async Task PublishAsync(PublishItem<PendingAssetCalculation> item)
        {
            await _channelWriter.WriteAsync(item);
        }
    }
}
