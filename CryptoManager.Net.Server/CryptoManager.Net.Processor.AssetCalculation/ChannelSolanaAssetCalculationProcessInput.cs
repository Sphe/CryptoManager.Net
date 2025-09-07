using CryptoManager.Net.Models;
using CryptoManager.Net.Publish;
using System.Threading.Channels;

namespace CryptoManager.Net.Processor.AssetCalculation
{
    public class ChannelSolanaAssetCalculationProcessInput : IProcessInput<PendingSolanaAssetCalculation>
    {
        private readonly ChannelReader<PublishItem<PendingSolanaAssetCalculation>> _channelWriter;

        public ChannelSolanaAssetCalculationProcessInput(ChannelReader<PublishItem<PendingSolanaAssetCalculation>> channelWriter)
        {
            _channelWriter = channelWriter;
        }

        public async Task<PublishItem<PendingSolanaAssetCalculation>?> ReadAsync(CancellationToken ct)
        {
            try
            {
                return await _channelWriter.ReadAsync(ct);
            }
            catch (OperationCanceledException) { return null; }
        }
    }
}
