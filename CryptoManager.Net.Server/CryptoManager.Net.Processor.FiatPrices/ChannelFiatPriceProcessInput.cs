using CryptoManager.Net.Models;
using CryptoManager.Net.Publish;
using System.Threading.Channels;

namespace CryptoManager.Net.Processor.FiatPrices
{
    public class ChannelFiatPriceProcessInput : IProcessInput<FiatPrice>
    {
        private readonly ChannelReader<PublishItem<FiatPrice>> _channelWriter;

        public ChannelFiatPriceProcessInput(ChannelReader<PublishItem<FiatPrice>> channelWriter)
        {
            _channelWriter = channelWriter;
        }

        public async Task<PublishItem<FiatPrice>?> ReadAsync(CancellationToken ct)
        {
            try
            {
                return await _channelWriter.ReadAsync(ct);
            }
            catch (OperationCanceledException) { return null; }
        }
    }
}
