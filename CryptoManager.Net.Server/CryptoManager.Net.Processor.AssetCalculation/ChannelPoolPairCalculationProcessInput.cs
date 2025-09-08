using CryptoManager.Net.Models;
using CryptoManager.Net.Publish;
using System.Threading.Channels;

namespace CryptoManager.Net.Processor.AssetCalculation
{
    public class ChannelPoolPairCalculationProcessInput : IProcessInput<PendingPoolPairCalculation>
    {
        private readonly ChannelReader<PublishItem<PendingPoolPairCalculation>> _reader;

        public ChannelPoolPairCalculationProcessInput(ChannelReader<PublishItem<PendingPoolPairCalculation>> reader)
        {
            _reader = reader;
        }

        public async Task<PublishItem<PendingPoolPairCalculation>?> ReadAsync(CancellationToken cancellationToken = default)
        {
            return await _reader.ReadAsync(cancellationToken);
        }
    }
}
