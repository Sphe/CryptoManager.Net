using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace CryptoManager.Net.Database
{
    public class MongoInitializationService : IHostedService
    {
        private readonly IMongoDatabase _database;
        private readonly ILogger<MongoInitializationService> _logger;

        public MongoInitializationService(IMongoDatabase database, ILogger<MongoInitializationService> logger)
        {
            _database = database;
            _logger = logger;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Initializing MongoDB indexes...");
                
                var context = new MongoTrackerContext(_database);
                await context.CreateIndexesAsync();
                
                _logger.LogInformation("MongoDB indexes created successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create MongoDB indexes.");
                throw;
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
