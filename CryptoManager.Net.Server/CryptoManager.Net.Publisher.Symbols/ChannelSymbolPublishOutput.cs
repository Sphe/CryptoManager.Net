using CryptoManager.Net.Models;
using System.Threading.Channels;

namespace CryptoManager.Net.Publish
{
    public class ChannelSymbolPublishOutput : IPublishOutput<Symbol>
    {
        private readonly ChannelWriter<PublishItem<Symbol>> _channelWriter;

        public ChannelSymbolPublishOutput(ChannelWriter<PublishItem<Symbol>> channelWriter)
        {
            _channelWriter = channelWriter;
        }

        public async Task PublishAsync(PublishItem<Symbol> item)
        {
            await _channelWriter.WriteAsync(item);
        }
    }
}
