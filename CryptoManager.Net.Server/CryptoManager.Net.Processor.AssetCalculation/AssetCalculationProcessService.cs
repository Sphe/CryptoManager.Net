using CryptoManager.Net.Database;
using CryptoManager.Net.Database.Models;
using CryptoManager.Net.Models;
using CryptoManager.Net.Publish;
using CryptoManager.Net.Services.External;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using System.Diagnostics;

namespace CryptoManager.Net.Processor.AssetCalculation
{
    public class AssetCalculationProcessService : IBackgroundService
    {
        private CancellationToken _stoppingToken = default;
        private readonly ILogger _logger;
        private readonly IProcessInput<PendingAssetCalculation> _processInput;
        private readonly IMongoDatabaseFactory _mongoDatabaseFactory;
        private readonly IJupiterTokenService _jupiterTokenService;
        private DateTime _lastLog;

        public AssetCalculationProcessService(
            ILogger<AssetCalculationProcessService> logger,
            IProcessInput<PendingAssetCalculation> processInput,
            IMongoDatabaseFactory mongoDatabaseFactory,
            IJupiterTokenService jupiterTokenService)
        {
            _logger = logger;
            _processInput = processInput;
            _mongoDatabaseFactory = mongoDatabaseFactory;
            _jupiterTokenService = jupiterTokenService;
        }

        public async Task ExecuteAsync(CancellationToken ct)
        {
            _stoppingToken = ct;

            _logger.LogDebug("Starting AssetCalculationService");
            while (!_stoppingToken.IsCancellationRequested)
            {
                var input = await _processInput.ReadAsync(ct);
                if (input != null)
                    await ProcessAsync(input);
            }

            _logger.LogDebug("AssetCalculationService stopped");
        }

        private async Task ProcessAsync(PublishItem<PendingAssetCalculation> update)
        {
            var sw = Stopwatch.StartNew();
            var context = _mongoDatabaseFactory.CreateContext();

            try
            {
                var ids = update.Data.Select(x => $"{x.Exchange}-{x.Asset}").ToList();
                
                // Get Jupiter tokens for contract address lookup
                var jupiterTokens = await _jupiterTokenService.GetVerifiedTokensAsync(_stoppingToken);
                var jupiterTokenDict = jupiterTokens
                    .GroupBy(t => t.Symbol, StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);
                
                // Get symbol data using MongoDB aggregation
                var symbolData = await context.ExchangeSymbols
                    .Aggregate()
                    .Match(x => ids.Contains(x.BaseAssetExchangeId))
                    .Group(x => new { x.Exchange, x.BaseAsset }, g => new
                    {
                        Exchange = g.Key.Exchange,
                        BaseAsset = g.Key.BaseAsset,
                        BaseAssetType = g.First().BaseAssetType,
                        QuoteAsset = g.First().QuoteAsset,
                        QuoteAssetType = g.First().QuoteAssetType,
                        ChangePercentage = g.First().ChangePercentage,
                        Volume = g.First().Volume,
                        LastPrice = g.First().LastPrice
                    })
                    .ToListAsync();

                // Create a lookup for the original asset names from PendingAssetCalculation
                // Use GroupBy to handle duplicate keys by taking the first occurrence
                var originalAssets = update.Data
                    .GroupBy(x => $"{x.Exchange}-{x.Asset}")
                    .ToDictionary(g => g.Key, g => g.First().Asset);
                
                var data = symbolData.Select(x =>
                {
                    var assetId = $"{x.Exchange}-{x.BaseAsset}";
                    var originalAssetName = originalAssets.TryGetValue(assetId, out var asset) ? asset : x.BaseAsset;
                    
                    // Get contract address from Jupiter token data using the original asset name
                    var jupiterToken = jupiterTokenDict.TryGetValue(originalAssetName, out var token) ? token : null;
                    
                    // Set default values
                    var blockchains = new[] { "other" };
                    var contractAddresses = Array.Empty<ContractAddress>();
                    
                    // If Jupiter token exists, set to Solana blockchain only
                    if (jupiterToken != null)
                    {
                        blockchains = new[] { "solana" };
                        contractAddresses = new[] { new ContractAddress { Network = "solana", Address = jupiterToken.Address } };
                    }

                    return new ExchangeAssetStats
                    {
                        Id = assetId,
                        Asset = originalAssetName,
                        Blockchains = blockchains,
                        ContractAddresses = contractAddresses,
                        AssetType = x.BaseAssetType,
                        Exchange = x.Exchange,
                        ChangePercentage = x.ChangePercentage,
                        Value = x.LastPrice,
                        Volume = x.Volume ?? 0,
                        UpdateTime = DateTime.UtcNow
                    };
                });

                // Bulk upsert using MongoDB
                var bulkOps = new List<WriteModel<ExchangeAssetStats>>();
                foreach (var assetStat in data)
                {
                    var filter = Builders<ExchangeAssetStats>.Filter.Eq(x => x.Id, assetStat.Id);
                    var updateDefinition = Builders<ExchangeAssetStats>.Update
                        .Set(x => x.Asset, assetStat.Asset)
                        .Set(x => x.Blockchains, assetStat.Blockchains)
                        .Set(x => x.ContractAddresses, assetStat.ContractAddresses)
                        .Set(x => x.AssetType, assetStat.AssetType)
                        .Set(x => x.Exchange, assetStat.Exchange)
                        .Set(x => x.ChangePercentage, assetStat.ChangePercentage)
                        .Set(x => x.Value, assetStat.Value)
                        .Set(x => x.Volume, assetStat.Volume)
                        .Set(x => x.JupiterPrice, assetStat.JupiterPrice)
                        .Set(x => x.ExchangePrice, assetStat.ExchangePrice)
                        .Set(x => x.PriceDifference, assetStat.PriceDifference)
                        .Set(x => x.UpdateTime, assetStat.UpdateTime);

                    bulkOps.Add(new UpdateOneModel<ExchangeAssetStats>(filter, updateDefinition) { IsUpsert = true });
                }

                if (bulkOps.Any())
                    await context.ExchangeAssetStats.BulkWriteAsync(bulkOps);

                sw.Stop();

                if (DateTime.UtcNow - _lastLog > TimeSpan.FromMinutes(1))
                {
                    _lastLog = DateTime.UtcNow;
                    _logger.LogInformation($"Asset calculation done in {sw.ElapsedMilliseconds}ms for {update.Data.Count()} items");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in asset calculation");
            }
        }
    }
}
