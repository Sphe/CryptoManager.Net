using CryptoManager.Net.Database;
using CryptoManager.Net.Database.Models;
using CryptoManager.Net.Publish;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace CryptoManager.Net.Analyzers
{
    public class UserPortfolioSnapshotService : IBackgroundService
    {
        private readonly ILogger _logger;
        private readonly IMongoDatabaseFactory _mongoDatabaseFactory;

        public UserPortfolioSnapshotService(ILogger<UserPortfolioSnapshotService> logger, IMongoDatabaseFactory mongoDatabaseFactory)
        {
            _mongoDatabaseFactory = mongoDatabaseFactory;
            _logger = logger;
        }

        public async Task ExecuteAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                var context = _mongoDatabaseFactory.CreateContext();
                try
                {
                    var allUserExchangeBalances = await context.AllUserExchangeBalancesAsync();
                    var allUserExternalBalances = await context.AllUserExternalBalancesAsync();
                    var timestamp = DateTime.UtcNow;

                    var data = new Dictionary<string, UserValuation>();
                    foreach(var item in allUserExchangeBalances)
                    {
                        data.Add(item.UserId, new UserValuation
                        {
                            Id = Guid.NewGuid().ToString(),
                            UserId = item.UserId,
                            Value = item.UsdValue,
                            Timestamp = timestamp
                        });
                    }

                    foreach(var item in allUserExternalBalances)
                    {
                        if (!data.TryGetValue(item.UserId, out var valuation))
                        {
                            valuation = new UserValuation { Id = Guid.NewGuid().ToString(), UserId = item.UserId, Timestamp = timestamp };
                            data.Add(item.UserId, valuation);
                        }

                        valuation.Value += item.UsdValue;
                    }

                    // Bulk upsert using MongoDB
                    var bulkOps = new List<WriteModel<UserValuation>>();
                    foreach (var valuation in data.Values)
                    {
                        var filter = Builders<UserValuation>.Filter.Eq(x => x.UserId, valuation.UserId) & 
                                   Builders<UserValuation>.Filter.Eq(x => x.Timestamp, valuation.Timestamp);
                        var update = Builders<UserValuation>.Update
                            .Set(x => x.Value, valuation.Value);

                        bulkOps.Add(new UpdateOneModel<UserValuation>(filter, update) { IsUpsert = true });
                    }

                    if (bulkOps.Any())
                        await context.UserValuations.BulkWriteAsync(bulkOps);
                }
                catch(Exception ex)
                {
                    _logger.LogError(ex, "Failed to process user portfolio snapshot");
                    return;
                }

                try { await Task.Delay(GetWait(), ct); } catch (Exception) { }
            }
        }

        private TimeSpan GetWait()
        {
            var time = DateTime.UtcNow;
            var offset = new DateTime(time.Year, time.Month, time.Day, time.Hour, 0, 0).AddHours(1);
            return offset - time;
        }
    }
}
