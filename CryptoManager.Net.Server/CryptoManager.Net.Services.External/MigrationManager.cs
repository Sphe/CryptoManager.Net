
using CryptoManager.Net.Database;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace CryptoManager.Net.Services.External
{
    public class MigrationManager : IHostedService
    {
        private readonly IMongoDatabaseFactory _mongoDatabaseFactory;
        private readonly ILogger _logger;

        public MigrationManager(
            ILogger<MigrationManager> logger,
            IMongoDatabaseFactory mongoDatabaseFactory)
        {
            _logger = logger;
            _mongoDatabaseFactory = mongoDatabaseFactory;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Initializing MongoDB database");

            try
            {
                var context = _mongoDatabaseFactory.CreateContext();
                await context.CreateIndexesAsync();
                _logger.LogInformation("MongoDB database initialized successfully");
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Failed to initialize MongoDB database; check DB accessibility");
                throw;
            }
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
