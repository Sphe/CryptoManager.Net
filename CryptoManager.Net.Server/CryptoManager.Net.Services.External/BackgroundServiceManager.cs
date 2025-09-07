using CryptoManager.Net.Publish;
using Microsoft.Extensions.Hosting;

namespace CryptoManager.Net.Services.External
{
    public class BackgroundServiceManager : IHostedService
    {
        private readonly IEnumerable<IBackgroundService> _backgroundServices;
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        private Task[]? _backgroundTasks;

        public BackgroundServiceManager(IEnumerable<IBackgroundService> backgroundServices)
        {
            _backgroundServices = backgroundServices;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _backgroundTasks = _backgroundServices.Select(x => x.ExecuteAsync(_cts.Token)).ToArray();
            return Task.CompletedTask;
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _cts.Cancel();
            await Task.WhenAll(_backgroundTasks!);
        }
    }
}
