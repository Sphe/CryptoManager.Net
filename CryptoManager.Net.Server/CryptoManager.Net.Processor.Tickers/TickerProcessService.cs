﻿using CryptoManager.Net.Database;
using CryptoManager.Net.Database.Models;
using CryptoManager.Net.Models;
using CryptoManager.Net.Publish;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace CryptoManager.Net.Processor.Tickers
{
    public class TickerProcessService : IBackgroundService
    {
        private CancellationToken _stoppingToken = default;
        private readonly ILogger _logger;
        private readonly IProcessInput<Ticker> _processInput;
        private readonly IPublishOutput<PendingAssetCalculation> _publishOutput;
        private readonly IMongoDatabaseFactory _mongoDatabaseFactory;

        public TickerProcessService(
            ILogger<TickerProcessService> logger,
            IProcessInput<Ticker> processInput,
            IPublishOutput<PendingAssetCalculation> publishOutput,
            IMongoDatabaseFactory mongoDatabaseFactory)
        {
            _logger = logger;
            _processInput = processInput;
            _publishOutput = publishOutput;
            _mongoDatabaseFactory = mongoDatabaseFactory;
        }

        public async Task ExecuteAsync(CancellationToken ct)
        {
            _stoppingToken = ct;

            _logger.LogDebug("Starting TickerService");
            while (!_stoppingToken.IsCancellationRequested)
            {
                var input = await _processInput.ReadAsync(ct);
                if (input != null)
                    await ProcessAsync(input);
            }

            _logger.LogDebug("TickerService stopped");
        }

        private async Task ProcessAsync(PublishItem<Ticker> update)
        {
            var context = _mongoDatabaseFactory.CreateContext();

            var symbols = new List<ExchangeSymbol>(update.Data.Count());
            foreach (var item in update.Data)
            {
                var symbol = new ExchangeSymbol
                {
                    Id = $"{item.Exchange}-{item.BaseAsset.ToUpperInvariant()}-{item.QuoteAsset.ToUpperInvariant()}",
                    ChangePercentage = item.ChangePercentage,
                    LastPrice = ClampValue(item.LastPrice),
                    HighPrice = ClampValue(item.HighPrice),
                    LowPrice = ClampValue(item.LowPrice),
                    Volume = ClampValue(item.Volume),
                    QuoteVolume = ClampValue(item.QuoteVolume),
                    UpdateTime = DateTime.UtcNow
                };

                // Try to derive some data if it's not returned by the API
                if (symbol.QuoteVolume == null)
                {
                    if (symbol.LowPrice.HasValue && symbol.HighPrice.HasValue)
                        // Take average of high/low price as avg price
                        symbol.QuoteVolume = (symbol.LowPrice.Value + symbol.HighPrice.Value) / 2 * symbol.Volume; 
                    if (symbol.ChangePercentage.HasValue)
                        // Take half of change percentage as avg price
                        symbol.QuoteVolume = (symbol.LastPrice * Math.Abs((symbol.ChangePercentage.Value / 200) - 1)) * symbol.Volume; 
                    else
                        // Fallback to last price as avg price     
                        symbol.QuoteVolume = symbol.LastPrice * symbol.Volume;                
                }

                if (symbol.Volume == null)
                {
                    if (symbol.LastPrice != null)
                        // Take the quote asset divided by the last price. Not very accurate..
                        symbol.Volume = symbol.QuoteVolume / symbol.LastPrice;
                }

                symbols.Add(symbol);
            }

            try
            {
                var bulkOps = new List<WriteModel<ExchangeSymbol>>();
                foreach (var symbol in symbols)
                {
                    var filter = Builders<ExchangeSymbol>.Filter.Eq(x => x.Id, symbol.Id);
                    var updateDefinition = Builders<ExchangeSymbol>.Update
                        .Set(x => x.ChangePercentage, symbol.ChangePercentage)
                        .Set(x => x.LastPrice, symbol.LastPrice)
                        .Set(x => x.HighPrice, symbol.HighPrice)
                        .Set(x => x.LowPrice, symbol.LowPrice)
                        .Set(x => x.Volume, symbol.Volume)
                        .Set(x => x.QuoteVolume, symbol.QuoteVolume)
                        .Set(x => x.UpdateTime, symbol.UpdateTime);

                    bulkOps.Add(new UpdateOneModel<ExchangeSymbol>(filter, updateDefinition) { IsUpsert = true });
                }

                if (bulkOps.Any())
                    await context.ExchangeSymbols.BulkWriteAsync(bulkOps);
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "Failed to process Ticker update");
            }

            await _publishOutput.PublishAsync(new PublishItem<PendingAssetCalculation>
            {
                Data = update.Data.Select(x => new PendingAssetCalculation { Exchange = x.Exchange, Asset = x.BaseAsset })
            });
        }

        private decimal? ClampValue(decimal? value)
        {
            if (value == null)
                return null;

            if (value > 99999999999999999999m)
                value = 99999999999999999999m;

            return Math.Round(value.Value, 8);
        }
    }
}
