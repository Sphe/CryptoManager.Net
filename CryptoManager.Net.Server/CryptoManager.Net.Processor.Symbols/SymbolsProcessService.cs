using CryptoManager.Net.Database;
using CryptoManager.Net.Database.Models;
using CryptoManager.Net.Models;
using CryptoManager.Net.Publish;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace CryptoManager.Net.Processor.Symbols
{
    public class SymbolsProcessService : IBackgroundService
    {
        private CancellationToken _stoppingToken = default;
        private readonly ILogger _logger;
        private string[] _usdStableAssets;
        private string[] _fiatAssets;
        private string[] _leveragedTokens;
        private readonly IProcessInput<Symbol> _processInput;
        private readonly IMongoDatabaseFactory _mongoDatabaseFactory;

        public SymbolsProcessService(
            IConfiguration configuration,
            ILogger<SymbolsProcessService> logger,
            IProcessInput<Symbol> publishOutput,
            IMongoDatabaseFactory mongoDatabaseFactory)
        {
            _logger = logger;
            _processInput = publishOutput;
            _mongoDatabaseFactory = mongoDatabaseFactory;

            _usdStableAssets = configuration.GetValue<string>("UsdStableAssets")!.Split(";");
            _fiatAssets = configuration.GetValue<string>("FiatAssets")!.Split(";");
            _leveragedTokens = configuration.GetValue<string>("LeveragedTokens")!.Split(";");
        }

        public async Task ExecuteAsync(CancellationToken ct)
        {
            _stoppingToken = ct;

            _logger.LogDebug("Starting SymbolService");
            while (!_stoppingToken.IsCancellationRequested)
            {
                var input = await _processInput.ReadAsync(ct);
                if (input != null)
                    await ProcessAsync(input);
            }

            _logger.LogDebug("SymbolService stopped");
        }

        private async Task ProcessAsync(PublishItem<Symbol> update)
        {
            var context = _mongoDatabaseFactory.CreateContext();

            // Get the names of all symbols for the exchange currently in the DB so we know which ones are no longer returned
            var exchangeSymbols = await context.ExchangeSymbols
                .Find(x => x.Exchange == update.Exchange)
                .Project(x => x.Id)
                .ToListAsync();
            var exchangeSymbolsSet = exchangeSymbols.ToHashSet();

            var symbols = new List<ExchangeSymbol>();
            foreach (var item in update.Data)
            {
                var id = $"{update.Exchange}-{item.BaseAsset.ToUpperInvariant()}-{item.QuoteAsset.ToUpperInvariant()}";
                var symbol = new ExchangeSymbol
                {
                    Id = id,

                    Exchange = update.Exchange!,
                    BaseAssetExchangeId = $"{update.Exchange}-{item.BaseAsset.ToUpperInvariant()}",
                    QuoteAssetExchangeId = $"{update.Exchange}-{item.QuoteAsset.ToUpperInvariant()}",
                    Name = item.Name,
                    BaseAsset = item.BaseAsset.ToUpperInvariant(),
                    QuoteAsset = item.QuoteAsset.ToUpperInvariant(),
                    MinNotionalValue = item.MinNotionalValue,
                    MinTradeQuantity = item.MinTradeQuantity,
                    PriceDecimals = item.PriceDecimals,
                    PriceSignificantFigures = item.PriceSignificantFigures,
                    QuantityDecimals = item.QuantityDecimals,
                    QuantityStep = item.QuantityStep,
                    BaseAssetType = GetAssetType(item.BaseAsset, true),
                    QuoteAssetType = GetAssetType(item.QuoteAsset, false),
                    Enabled = item.Trading,
                    UpdateTime = DateTime.UtcNow,
                    DeleteTime = null
                };

                symbols.Add(symbol);
                exchangeSymbolsSet.Remove(id);
            }

            // Any symbols no longer returned should be set to deleted in DB
            var symbolsToRemove = new List<ExchangeSymbol>();
            foreach (var item in exchangeSymbolsSet)
            {
                symbolsToRemove.Add(new ExchangeSymbol
                {
                    Id = item,
                    Exchange = update.Exchange!,
                    LastPrice = null,
                    HighPrice = null,
                    LowPrice = null,
                    Volume = 0,
                    QuoteVolume = 0,
                    ChangePercentage = null,
                    Enabled = false,
                    UpdateTime = DateTime.UtcNow,
                    DeleteTime = DateTime.UtcNow
                });
            }

            try
            {
                var bulkOps = new List<WriteModel<ExchangeSymbol>>();
                foreach (var symbol in symbols)
                {
                    var filter = Builders<ExchangeSymbol>.Filter.Eq(x => x.Id, symbol.Id);
                    var updateDefinition = Builders<ExchangeSymbol>.Update
                        .Set(x => x.Exchange, symbol.Exchange)
                        .Set(x => x.Name, symbol.Name)
                        .Set(x => x.BaseAsset, symbol.BaseAsset)
                        .Set(x => x.BaseAssetExchangeId, symbol.BaseAssetExchangeId)
                        .Set(x => x.QuoteAsset, symbol.QuoteAsset)
                        .Set(x => x.QuoteAssetExchangeId, symbol.QuoteAssetExchangeId)
                        .Set(x => x.MinNotionalValue, symbol.MinNotionalValue)
                        .Set(x => x.MinTradeQuantity, symbol.MinTradeQuantity)
                        .Set(x => x.PriceDecimals, symbol.PriceDecimals)
                        .Set(x => x.PriceSignificantFigures, symbol.PriceSignificantFigures)
                        .Set(x => x.QuantityDecimals, symbol.QuantityDecimals)
                        .Set(x => x.QuantityStep, symbol.QuantityStep)
                        .Set(x => x.BaseAssetType, symbol.BaseAssetType)
                        .Set(x => x.QuoteAssetType, symbol.QuoteAssetType)
                        .Set(x => x.Enabled, symbol.Enabled)
                        .Set(x => x.UpdateTime, symbol.UpdateTime)
                        .Set(x => x.DeleteTime, symbol.DeleteTime);

                    bulkOps.Add(new UpdateOneModel<ExchangeSymbol>(filter, updateDefinition) { IsUpsert = true });
                }

                if (bulkOps.Any())
                    await context.ExchangeSymbols.BulkWriteAsync(bulkOps);
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "Failed to process updated symbols in Symbol update");
            }

            try
            {
                var bulkOpsRemove = new List<WriteModel<ExchangeSymbol>>();
                foreach (var symbol in symbolsToRemove)
                {
                    var filter = Builders<ExchangeSymbol>.Filter.Eq(x => x.Id, symbol.Id);
                    var updateDefinition = Builders<ExchangeSymbol>.Update
                        .Set(x => x.Exchange, symbol.Exchange)
                        .Set(x => x.LastPrice, symbol.LastPrice)
                        .Set(x => x.HighPrice, symbol.HighPrice)
                        .Set(x => x.LowPrice, symbol.LowPrice)
                        .Set(x => x.Volume, symbol.Volume)
                        .Set(x => x.QuoteVolume, symbol.QuoteVolume)
                        .Set(x => x.ChangePercentage, symbol.ChangePercentage)
                        .Set(x => x.Enabled, symbol.Enabled)
                        .Set(x => x.UpdateTime, symbol.UpdateTime)
                        .Set(x => x.DeleteTime, symbol.DeleteTime);

                    bulkOpsRemove.Add(new UpdateOneModel<ExchangeSymbol>(filter, updateDefinition) { IsUpsert = true });
                }

                if (bulkOpsRemove.Any())
                    await context.ExchangeSymbols.BulkWriteAsync(bulkOpsRemove);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process removed symbols in Symbol update");
            }
        }

        private AssetType GetAssetType(string asset, bool checkLeveraged)
        {
            if (_usdStableAssets.Contains(asset))
                return AssetType.Stable;

            if (_fiatAssets.Contains(asset))
                return AssetType.Fiat;

            if (checkLeveraged && _leveragedTokens.Any(x => asset.Contains(x)))
                return AssetType.LeveragedToken;

            return AssetType.Crypto;
        }

    }
}
