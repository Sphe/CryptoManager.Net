using CryptoManager.Net.Database;
using CryptoManager.Net.Database.Models;
using CryptoManager.Net.Publish;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace CryptoManager.Net.Analyzers
{
    public class SymbolUsdVolumeService : IBackgroundService
    {
        private ILogger _logger;
        private readonly IMongoDatabaseFactory _mongoDatabaseFactory;

        public SymbolUsdVolumeService(ILogger<SymbolUsdVolumeService> logger, IMongoDatabaseFactory mongoDatabaseFactory)
        {
            _logger = logger;
            _mongoDatabaseFactory = mongoDatabaseFactory;
        }

        public async Task ExecuteAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                await ProcessAsync(ct);
                try { await Task.Delay(5000, ct); } catch { }
            }

        }

        private async Task ProcessAsync(CancellationToken ct)
        {
            try
            {
                var context = _mongoDatabaseFactory.CreateContext();

                // Get fiat prices
                var fiatPrices = await context.FiatPrices.Find(_ => true).ToListAsync();

                // Get all symbols we need to process
                var allSymbolVolumes = await context.Symbols
                    .Find(_ => true)
                    .Project(x => new { x.Id, x.Exchange, x.QuoteAsset, x.QuoteVolume })
                    .ToListAsync();
                
                // Get all exchange quote asset  prices
                var quoteSymbols = allSymbolVolumes.GroupBy(x => new { x.Exchange, x.QuoteAsset }).Select(x => $"{x.Key.Exchange}-{x.Key.QuoteAsset}").ToList();

                // Get all quote asset exchanges prices we need
                var quoteSymbolPrices = await context.ExchangeAssetStats
                    .Find(x => quoteSymbols.Contains(x.Id))
                    .Project(x => new { x.Id, x.Value })
                    .ToListAsync();

                var data = new List<ExchangeSymbol>(allSymbolVolumes.Count);
                foreach(var symbol in allSymbolVolumes)
                {
                    // If the price of the quote asset is known/traded on the exchange it self, use that
                    var quotePrice = quoteSymbolPrices.SingleOrDefault(x => x.Id == symbol.Exchange + symbol.QuoteAsset)?.Value;
                    if (quotePrice == null)
                    {
                        // If the quote asset is a fiat currency use the exchange rate for that
                        var fiatPrice = fiatPrices.SingleOrDefault(x => x.Id == symbol.QuoteAsset);
                        if (fiatPrice != null)
                        {
                            quotePrice = 1 / fiatPrice.Price;
                        }
                        else
                        {
                            // If there are other exchanges trading this quote asset use the average price from those
                            var otherExchangePrices = quoteSymbolPrices.Where(x => x.Value != null && x.Id.EndsWith($"-{symbol.QuoteAsset}")).ToList();
                            if (!otherExchangePrices.Any())
                                continue;

                            quotePrice = otherExchangePrices.Average(x => x.Value);
                        }
                    }

                    data.Add(new ExchangeSymbol
                    {
                        Id = symbol.Id,
                        UsdVolume = symbol.QuoteVolume * quotePrice!.Value,
                        UpdateTime = DateTime.UtcNow
                    });
                }

                // Bulk upsert using MongoDB
                var bulkOps = new List<WriteModel<ExchangeSymbol>>();
                foreach (var symbol in data)
                {
                    var filter = Builders<ExchangeSymbol>.Filter.Eq(x => x.Id, symbol.Id);
                    var update = Builders<ExchangeSymbol>.Update
                        .Set(x => x.UsdVolume, symbol.UsdVolume)
                        .Set(x => x.UpdateTime, symbol.UpdateTime);

                    bulkOps.Add(new UpdateOneModel<ExchangeSymbol>(filter, update) { IsUpsert = true });
                }

                if (bulkOps.Any())
                    await context.Symbols.BulkWriteAsync(bulkOps);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during Symbols USD volume calculation");
            }
        }
    }
}
