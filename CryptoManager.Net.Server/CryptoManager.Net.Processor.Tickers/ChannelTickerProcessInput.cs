using CryptoManager.Net.Models;
using CryptoManager.Net.Publish;
using System.Threading.Channels;

namespace CryptoManager.Net.Processor.Tickers
{
    public class ChannelTickerProcessInput : IProcessInput<Ticker>
    {
        private readonly ChannelReader<PublishItem<Ticker>> _channelWriter;

        public ChannelTickerProcessInput(ChannelReader<PublishItem<Ticker>> channelWriter)
        {
            _channelWriter = channelWriter;
        }

        public async Task<PublishItem<Ticker>?> ReadAsync(CancellationToken ct)
        {
            try
            {
                return await _channelWriter.ReadAsync(ct);
            }
            catch (OperationCanceledException) { return null; }
        }
    }
}
