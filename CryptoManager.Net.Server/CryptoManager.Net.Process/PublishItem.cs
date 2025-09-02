using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CryptoExchange.Net.Tracker.Publish
{
    public record PublishItem<T>
    {
        public string Exchange { get; set; }
        public IEnumerable<T> Data { get; set; }
        public DateTime Timestamp { get; set; }

        public PublishItem(string exchange)
        {
            Exchange = exchange;
            Timestamp = DateTime.UtcNow;
        }
    }
}
