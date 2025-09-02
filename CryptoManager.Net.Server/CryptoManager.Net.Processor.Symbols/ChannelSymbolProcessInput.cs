using CryptoManager.Net.Models;
using CryptoManager.Net.Publish;
using System.Threading.Channels;

namespace CryptoManager.Net.Processor.Symbols
{
    public class ChannelSymbolProcessInput : IProcessInput<Symbol>
    {
        private readonly ChannelReader<PublishItem<Symbol>> _channelWriter;

        public ChannelSymbolProcessInput(ChannelReader<PublishItem<Symbol>> channelWriter)
        {
            _channelWriter = channelWriter;
        }

        public async Task<PublishItem<Symbol>?> ReadAsync(CancellationToken ct)
        {
            try
            {
                return await _channelWriter.ReadAsync(ct);
            }
            catch (OperationCanceledException) { return null; }
        }
    }
}
