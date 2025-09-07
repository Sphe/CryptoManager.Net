using CryptoManager.Net.Models;
using CryptoManager.Net.Publish;
using System.Threading.Channels;

namespace CryptoManager.Net.Processor.AssetCalculation
{
    public class ChannelSolanaAssetCalculationPublishOutput : IPublishOutput<PendingSolanaAssetCalculation>
    {
        private readonly ChannelWriter<PublishItem<PendingSolanaAssetCalculation>> _channelWriter;

        public ChannelSolanaAssetCalculationPublishOutput(ChannelWriter<PublishItem<PendingSolanaAssetCalculation>> channelWriter)
        {
            _channelWriter = channelWriter;
        }

        public async Task PublishAsync(PublishItem<PendingSolanaAssetCalculation> item)
        {
            await _channelWriter.WriteAsync(item);
        }
    }
}
