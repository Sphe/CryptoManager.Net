using CryptoManager.Net.Models;
using CryptoManager.Net.Publish;
using System.Threading.Channels;

namespace CryptoManager.Net.Publisher.Tickers
{
    public class ChannelTickerPublishOutput : IPublishOutput<Ticker>
    {
        private readonly ChannelWriter<PublishItem<Ticker>> _channelWriter;

        public ChannelTickerPublishOutput(ChannelWriter<PublishItem<Ticker>> channelWriter)
        {
            _channelWriter = channelWriter;
        }

        public async Task PublishAsync(PublishItem<Ticker> item)
        {
            await _channelWriter.WriteAsync(item);
        }
    }
}
