using CryptoManager.Net.Database;
using CryptoManager.Net.Database.Models;
using CryptoManager.Net.Publish;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Diagnostics;

namespace CryptoManager.Net.Processor.AssetCalculation
{
    public class AssetAggregationService : IBackgroundService
    {
        private CancellationToken _stoppingToken = default;
        private readonly ILogger _logger;
        private readonly IMongoDatabaseFactory _mongoDatabaseFactory;
        private DateTime _lastLog;
        private readonly TimeSpan _aggregationInterval = TimeSpan.FromMinutes(5);

        public AssetAggregationService(
            ILogger<AssetAggregationService> logger,
            IMongoDatabaseFactory mongoDatabaseFactory)
        {
            _logger = logger;
            _mongoDatabaseFactory = mongoDatabaseFactory;
        }

        public async Task ExecuteAsync(CancellationToken ct)
        {
            _stoppingToken = ct;

            _logger.LogDebug("Starting AssetAggregationService");
            while (!_stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await AggregateAssetsAsync();
                    await Task.Delay(_aggregationInterval, _stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in AssetAggregationService");
                    await Task.Delay(TimeSpan.FromMinutes(1), _stoppingToken);
                }
            }

            _logger.LogDebug("AssetAggregationService stopped");
        }

        private async Task AggregateAssetsAsync()
        {
            var sw = Stopwatch.StartNew();
            var context = _mongoDatabaseFactory.CreateContext();

            try
            {
                // Aggregate exchange asset stats into unique asset stats
                var aggregatedAssets = await context.ExchangeAssetStats
                    .Aggregate()
                    .Group(x => x.Asset, g => new AssetStats
                    {
                        Asset = g.Key,
                        Blockchains = g.SelectMany(x => x.Blockchains).Distinct().ToArray(),
                        ContractAddresses = g.SelectMany(x => x.ContractAddresses).Distinct().ToArray(),
                        AssetType = g.First().AssetType,
                        Value = g.Average(x => x.Value),
                        Volume = g.Sum(x => x.Volume),
                        ChangePercentage = g.Average(x => x.ChangePercentage),
                        JupiterPrice = g.Average(x => x.JupiterPrice),
                        ExchangePrice = g.Average(x => x.ExchangePrice),
                        PriceDifference = g.Average(x => x.PriceDifference),
                        UpdateTime = g.Max(x => x.UpdateTime),
                        ExchangeCount = g.Count(),
                        Exchanges = g.Select(x => x.Exchange).Distinct().ToArray()
                    })
                    .ToListAsync();

                if (aggregatedAssets.Any())
                {
                    // Bulk upsert aggregated assets
                    var bulkOps = new List<WriteModel<AssetStats>>();
                    foreach (var asset in aggregatedAssets)
                    {
                    var filter = Builders<AssetStats>.Filter.Eq(x => x.Asset, asset.Asset);
                    var updateDefinition = Builders<AssetStats>.Update
                        .Set(x => x.Blockchains, asset.Blockchains)
                        .Set(x => x.ContractAddresses, asset.ContractAddresses)
                        .Set(x => x.AssetType, asset.AssetType)
                        .Set(x => x.Value, asset.Value)
                        .Set(x => x.Volume, asset.Volume)
                        .Set(x => x.ChangePercentage, asset.ChangePercentage)
                        .Set(x => x.JupiterPrice, asset.JupiterPrice)
                        .Set(x => x.ExchangePrice, asset.ExchangePrice)
                        .Set(x => x.PriceDifference, asset.PriceDifference)
                        .Set(x => x.UpdateTime, asset.UpdateTime)
                        .Set(x => x.ExchangeCount, asset.ExchangeCount)
                        .Set(x => x.Exchanges, asset.Exchanges);

                        bulkOps.Add(new UpdateOneModel<AssetStats>(filter, updateDefinition) { IsUpsert = true });
                    }

                    await context.AssetStats.BulkWriteAsync(bulkOps);
                }

                // Clean up obsolete assets that no longer exist in ExchangeAssetStats
                await CleanupObsoleteAssetsAsync(context, aggregatedAssets);

                sw.Stop();

                if (DateTime.UtcNow - _lastLog > TimeSpan.FromMinutes(1))
                {
                    _lastLog = DateTime.UtcNow;
                    _logger.LogInformation($"Asset aggregation done in {sw.ElapsedMilliseconds}ms for {aggregatedAssets.Count} unique assets");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in asset aggregation");
            }
        }

        private async Task CleanupObsoleteAssetsAsync(MongoTrackerContext context, List<AssetStats> currentAssets)
        {
            try
            {
                // Get all current asset keys (Asset names)
                var currentAssetKeys = currentAssets
                    .Select(a => a.Asset)
                    .ToHashSet();

                // Find assets in AssetStats that are no longer in ExchangeAssetStats
                var allAssetStats = await context.AssetStats
                    .Find(Builders<AssetStats>.Filter.Empty)
                    .ToListAsync();

                var obsoleteAssets = allAssetStats
                    .Where(a => !currentAssetKeys.Contains(a.Asset))
                    .ToList();

                if (obsoleteAssets.Any())
                {
                    // Delete obsolete assets
                    var deleteFilter = Builders<AssetStats>.Filter.Or(
                        obsoleteAssets.Select(a => 
                            Builders<AssetStats>.Filter.Eq(x => x.Asset, a.Asset)
                        ).ToArray()
                    );

                    var deleteResult = await context.AssetStats.DeleteManyAsync(deleteFilter);

                    _logger.LogInformation("AssetAggregationService removed {ObsoleteCount} obsolete assets from AssetStats: {ObsoleteAssets}", 
                        deleteResult.DeletedCount, 
                        string.Join(", ", obsoleteAssets.Take(10).Select(a => a.Asset)));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cleaning up obsolete assets in AssetStats");
            }
        }
    }
}
