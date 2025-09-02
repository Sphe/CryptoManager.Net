using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CryptoExchange.Net.Tracker.Publish
{
    public interface IBackgroundService
    {
        Task StartAsync(CancellationToken ct);
    }
}
