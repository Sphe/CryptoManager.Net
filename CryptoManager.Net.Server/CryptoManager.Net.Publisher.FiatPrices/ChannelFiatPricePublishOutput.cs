using CryptoManager.Net.Models;
using CryptoManager.Net.Publish;
using System.Threading.Channels;

namespace CryptoManager.Net.Publisher.FiatPrices
{
    public class ChannelFiatPricePublishOutput : IPublishOutput<FiatPrice>
    {
        private readonly ChannelWriter<PublishItem<FiatPrice>> _channelWriter;

        public ChannelFiatPricePublishOutput(ChannelWriter<PublishItem<FiatPrice>> channelWriter)
        {
            _channelWriter = channelWriter;
        }

        public async Task PublishAsync(PublishItem<FiatPrice> item)
        {
            await _channelWriter.WriteAsync(item);
        }
    }
}
