using CryptoManager.Net.Models;
using CryptoManager.Net.Publish;
using System.Threading.Channels;

namespace CryptoManager.Net.Processor.AssetCalculation
{
    public class ChannelPoolPairCalculationPublishOutput : IPublishOutput<PendingPoolPairCalculation>
    {
        private readonly ChannelWriter<PublishItem<PendingPoolPairCalculation>> _writer;

        public ChannelPoolPairCalculationPublishOutput(ChannelWriter<PublishItem<PendingPoolPairCalculation>> writer)
        {
            _writer = writer;
        }

        public async Task PublishAsync(PublishItem<PendingPoolPairCalculation> item)
        {
            await _writer.WriteAsync(item);
        }
    }
}
