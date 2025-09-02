using CryptoManager.Net.Models;
using CryptoManager.Net.Publish;
using System.Threading.Channels;

namespace CryptoManager.Net.Processor.Tickers
{
    public class ChannelAssetCalculationProcessInput : IProcessInput<PendingAssetCalculation>
    {
        private readonly ChannelReader<PublishItem<PendingAssetCalculation>> _channelWriter;

        public ChannelAssetCalculationProcessInput(ChannelReader<PublishItem<PendingAssetCalculation>> channelWriter)
        {
            _channelWriter = channelWriter;
        }

        public async Task<PublishItem<PendingAssetCalculation>?> ReadAsync(CancellationToken ct)
        {
            try
            {
                return await _channelWriter.ReadAsync(ct);
            }
            catch (OperationCanceledException) { return null; }
        }
    }
}
