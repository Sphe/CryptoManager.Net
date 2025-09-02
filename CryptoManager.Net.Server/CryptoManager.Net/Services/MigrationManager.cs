
using CryptoManager.Net.Database;
using Microsoft.EntityFrameworkCore;

namespace CryptoManager.Net.Services
{
    public class MigrationManager : IHostedService
    {
        private readonly IDbContextFactory<TrackerContext> _contextFactory;
        private readonly ILogger _logger;
        // Should probably be a setting. Can be false if migrations are managed through a CI/CD pipeline
        private bool _applyMigrations = true;

        public MigrationManager(
            ILogger<MigrationManager> logger,
            IDbContextFactory<TrackerContext> contextFactory)
        {
            _logger = logger;
            _contextFactory = contextFactory;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Checking for migrations");

            var context = _contextFactory.CreateDbContext();
            IEnumerable<string>? pending = null;
            try
            {
                pending = await context.Database.GetPendingMigrationsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Failed to check for pending migrations; check DB accessibility");
                throw;
            }

            if (pending.Any())
            {
                _logger.LogInformation("Migrations pending:");
                foreach (var migration in pending)
                    _logger.LogInformation(migration);


                if (_applyMigrations)
                {
                    _logger.LogInformation("Applying pending migrations");
                    await context.Database.MigrateAsync();

                    _logger.LogInformation("Pending migrations applied");
                }
            }
            else
            {
                _logger.LogInformation("No pending migrations");
            }
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
