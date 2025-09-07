using CryptoClients.Net.Enums;
using CryptoClients.Net.Interfaces;
using CryptoExchange.Net.Objects;
using CryptoExchange.Net.Objects.Errors;
using CryptoExchange.Net.Objects.Sockets;
using CryptoExchange.Net.SharedApis;
using CryptoManager.Net.Data;
using CryptoManager.Net.Database;
using CryptoManager.Net.Database.Models;
using CryptoManager.Net.Models;
using CryptoManager.Net.Publish;
using CryptoManager.Net.Services.External;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using System.Diagnostics;

namespace CryptoManager.Net.Publisher.Tickers
{
    public class TickerPublishService : IBackgroundService
    {
        private CancellationToken _stoppingToken = default;
        private readonly ILogger _logger;
        private readonly IExchangeRestClient _restClient;
        private readonly IExchangeSocketClient _socketClient;
        private readonly IPublishOutput<Ticker> _publishOutput;
        private readonly IPublishOutput<PendingAssetCalculation> _assetCalculationOutput;
        private readonly IPublishOutput<PendingSolanaAssetCalculation> _solanaAssetCalculationOutput;
        private readonly IJupiterTokenService _jupiterTokenService;
        private readonly IMongoDatabaseFactory _mongoDatabaseFactory;
        private readonly double _pollInterval;
        private readonly DataBatcher<Ticker> _tickerBatcher;
        private readonly Dictionary<string, SharedSpotSymbol[]> _symbols = new Dictionary<string, SharedSpotSymbol[]>();
        private readonly AsyncResetEvent _symbolsInitialSetEvent = new AsyncResetEvent(false, false);

        private readonly string[]? _enabledExchanges;

        public TickerPublishService(
            ILogger<TickerPublishService> logger,
            IConfiguration configuration,
            IExchangeRestClient restClient,
            IExchangeSocketClient socketClient,
            IPublishOutput<Ticker> publishOutput,
            IPublishOutput<PendingAssetCalculation> assetCalculationOutput,
            IPublishOutput<PendingSolanaAssetCalculation> solanaAssetCalculationOutput,
            IJupiterTokenService jupiterTokenService,
            IMongoDatabaseFactory mongoDatabaseFactory)
        {
            _logger = logger;
            _restClient = restClient;
            _socketClient = socketClient;
            _publishOutput = publishOutput;
            _assetCalculationOutput = assetCalculationOutput;
            _solanaAssetCalculationOutput = solanaAssetCalculationOutput;
            _jupiterTokenService = jupiterTokenService;
            _mongoDatabaseFactory = mongoDatabaseFactory;

            _pollInterval = configuration.GetValue<double?>("TickersPollInterval") ?? 0.16;
            _enabledExchanges = configuration.GetValue<string?>("EnabledExchanges")?.Split(";");

            _tickerBatcher = new DataBatcher<Ticker>(TimeSpan.FromSeconds(5), PublishTickers);
        }

        private async Task PublishTickers(Dictionary<string, Ticker> dictionary)
        {
            var exchangeData = new PublishItem<Ticker>();
            exchangeData.Data = dictionary.Values;
            await _publishOutput.PublishAsync(exchangeData);
        }

        public async Task ExecuteAsync(CancellationToken ct)
        {
            _stoppingToken = ct;

            _logger.LogDebug("Starting TickerPublishService");

            await _tickerBatcher.StartAsync();

            // Init Symbol Cache
            _ = PollSymbolsAsync();
            await _symbolsInitialSetEvent.WaitAsync();

            // Subscribe websocket which exchange support this
            var subscribedExchanges = await SubscribeTickersAsync(_stoppingToken);
            var pollingExchanges = Exchange.All.Except(subscribedExchanges).ToList();
            if (_enabledExchanges?.Any() == true)
                pollingExchanges = pollingExchanges.Where(x => _enabledExchanges.Contains(x)).ToList();

            _logger.LogInformation("TickerPublishService {SubCount} exchange subscribed, starting polling for {PollCount}", subscribedExchanges.Count, pollingExchanges.Count);

            // For remaining exchanges use polling
            while (!_stoppingToken.IsCancellationRequested)
            {
                var sw = Stopwatch.StartNew();
                await PollAsync(pollingExchanges);

                var waitTime = TimeSpan.FromMinutes(_pollInterval);
                waitTime = waitTime.Add(-sw.Elapsed);
                if (waitTime < TimeSpan.FromMilliseconds(1))
                    waitTime = TimeSpan.FromMilliseconds(1);

                try { await Task.Delay(waitTime, _stoppingToken); } catch { }
            }

            await _tickerBatcher.StopAsync();
            _logger.LogDebug("TickerPublishService stopped");
        }

        private async Task PollSymbolsAsync()
        {
            // Poll symbols on interval so we know which ones are no longer support
            while (!_stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // Get verified tokens from Jupiter
                    var jupiterTokens = await _jupiterTokenService.GetVerifiedTokensAsync(_stoppingToken);
                    var jupiterSymbols = jupiterTokens.Select(t => t.Symbol).ToHashSet(StringComparer.OrdinalIgnoreCase);
                    
                    _logger.LogInformation("TickerPublishService loaded {Count} verified Jupiter tokens for filtering", jupiterSymbols.Count);

                    var results = await _restClient.GetSpotSymbolsAsync(new GetSymbolsRequest(), _enabledExchanges);

                    foreach (var result in results.Where(x => x.Success))
                    {
                        // Get existing symbols for this exchange from database
                        var context = _mongoDatabaseFactory.CreateContext();
                        var existingSymbolIds = await context.ExchangeAssetStats
                            .Find(x => x.Exchange == result.Exchange)
                            .Project(x => x.Id)
                            .ToListAsync();
                        
                        var existingSymbolsSet = existingSymbolIds.ToHashSet();
                        
                        // Filter to only new symbols that haven't been seen before
                        var newSymbols = result.Data.Where(s => 
                        {
                            var symbolId = $"{result.Exchange}-{s.BaseAsset}";
                            return !existingSymbolsSet.Contains(symbolId);
                        }).ToArray();
                        
                        var existingSymbols = result.Data.Where(s => 
                        {
                            var symbolId = $"{result.Exchange}-{s.BaseAsset}";
                            return existingSymbolsSet.Contains(symbolId);
                        }).ToArray();
                        
                        _logger.LogInformation("TickerPublishService found {NewCount} new symbols and {ExistingCount} existing symbols for {Exchange}", 
                            newSymbols.Length, existingSymbols.Length, result.Exchange);
                        
                        List<SymbolValidationResult> validationResults = new();
                        List<SharedSpotSymbol> allValidatedSymbols = new();
                        List<SharedSpotSymbol> allNonValidatedSymbols = new();
                        
                        // Only validate new symbols
                        if (newSymbols.Any())
                        {
                            validationResults = (await ValidateSymbolsWithPriceAsync(result.Exchange, newSymbols, jupiterTokens)).ToList();
                            
                            // Separate validated and non-validated symbols
                            var validatedSymbols = validationResults.Where(r => r.IsValidated).Select(r => r.Symbol).ToArray();
                            var nonValidatedSymbols = validationResults.Where(r => !r.IsValidated).Select(r => r.Symbol).ToArray();
                            
                            allValidatedSymbols.AddRange(validatedSymbols);
                            allNonValidatedSymbols.AddRange(nonValidatedSymbols);
                            
                            // Publish Solana asset calculations for validated new symbols
                            var solanaAssetCalculations = validationResults
                                .Where(r => r.IsValidated)
                                .Select(r => new PendingSolanaAssetCalculation
                                {
                                    Exchange = result.Exchange,
                                    Asset = r.Symbol.BaseAsset,
                                    Blockchains = r.Blockchains,
                                    ContractAddresses = r.ContractAddresses,
                                    JupiterPrice = r.JupiterPrice,
                                    ExchangePrice = r.ExchangePrice,
                                    PriceDifference = r.PriceDifference
                                })
                                .ToList();
                            
                            if (solanaAssetCalculations.Any())
                            {
                                await _solanaAssetCalculationOutput.PublishAsync(new PublishItem<PendingSolanaAssetCalculation>
                                {
                                    Data = solanaAssetCalculations
                                });
                            }
                            
                            // Publish regular asset calculations for non-validated new symbols
                            var regularAssetCalculations = nonValidatedSymbols
                                .Select(s => new PendingAssetCalculation
                                {
                                    Exchange = result.Exchange,
                                    Asset = s.BaseAsset
                                })
                                .ToList();
                            
                            if (regularAssetCalculations.Any())
                            {
                                await _assetCalculationOutput.PublishAsync(new PublishItem<PendingAssetCalculation>
                                {
                                    Data = regularAssetCalculations
                                });
                            }
                        }
                        
                        // For existing symbols, we need to determine their validation status from the database
                        if (existingSymbols.Any())
                        {
                            var existingSolanaSymbols = await context.ExchangeAssetStats
                                .Find(x => x.Exchange == result.Exchange && x.Blockchains.Contains("solana"))
                                .Project(x => x.Asset)
                                .ToListAsync();
                            
                            var existingSolanaSet = existingSolanaSymbols.ToHashSet(StringComparer.OrdinalIgnoreCase);
                            
                            // Add existing symbols to the appropriate lists based on their blockchain type
                            foreach (var symbol in existingSymbols)
                            {
                                if (existingSolanaSet.Contains(symbol.BaseAsset))
                                {
                                    allValidatedSymbols.Add(symbol);
                                }
                                else
                                {
                                    allNonValidatedSymbols.Add(symbol);
                                }
                            }
                        }
                        
                        _symbols[result.Exchange] = allValidatedSymbols.ToArray();
                        
                        // Remove obsolete symbols that are no longer returned by the exchange
                        var currentSymbolIds = result.Data.Select(s => $"{result.Exchange}-{s.BaseAsset}").ToHashSet();
                        var obsoleteSymbolIds = existingSymbolsSet.Except(currentSymbolIds).ToList();
                        
                        if (obsoleteSymbolIds.Any())
                        {
                            var deleteResult = await context.ExchangeAssetStats.DeleteManyAsync(
                                Builders<ExchangeAssetStats>.Filter.In(x => x.Id, obsoleteSymbolIds)
                            );
                            
                            _logger.LogInformation("TickerPublishService removed {ObsoleteCount} obsolete symbols from {Exchange}: {ObsoleteSymbols}", 
                                deleteResult.DeletedCount, result.Exchange, string.Join(", ", obsoleteSymbolIds.Take(10)));
                        }
                        
                        _logger.LogInformation("TickerPublishService processed {NewValidatedCount} new validated, {NewNonValidatedCount} new non-validated, {ExistingCount} existing, and removed {ObsoleteCount} obsolete symbols for {Exchange}", 
                            validationResults.Count(r => r.IsValidated), 
                            validationResults.Count(r => !r.IsValidated), 
                            existingSymbols.Length, 
                            obsoleteSymbolIds.Count,
                            result.Exchange);
                    }

                    _symbolsInitialSetEvent.Set();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "TickerPublishService error in PollSymbolsAsync, will retry in next cycle");
                }

                try { await Task.Delay(TimeSpan.FromMinutes(15), _stoppingToken); } catch { }
            }
        }

        private async Task<List<string>> SubscribeTickersAsync(CancellationToken ct)
        {
            var exchanges = _enabledExchanges ?? Exchange.All;
            var subbedExchanges = new List<string>();
            var allTickerClients = _socketClient.GetTickersClients(TradingMode.Spot).Where(x => exchanges.Contains(x.Exchange));

            foreach(var tickerClient in allTickerClients)
            {
                _logger.LogDebug("TickerPublishService starting all ticker for {Exchange}", tickerClient.Exchange);
                var subResult = await tickerClient.SubscribeToAllTickersUpdatesAsync(new SubscribeAllTickersRequest(), ProcessUpdate, _stoppingToken);
                if (subResult)
                    subbedExchanges.Add(subResult.Exchange);
            }

            var multiTickerClients = _socketClient.GetTickerClients(TradingMode.Spot)
                .Where(x => !subbedExchanges.Contains(x.Exchange) && exchanges.Contains(x.Exchange) && x.SubscribeTickerOptions.SupportsMultipleSymbols);
            foreach (var tickerClient in multiTickerClients)
            {
                if (!_symbols!.TryGetValue(tickerClient.Exchange, out var exchangeSymbols))
                    continue;

                var offset = 0;
                var perPage = tickerClient.SubscribeTickerOptions.MaxSymbolCount ?? exchangeSymbols.Length;
                var pages = Math.Ceiling(exchangeSymbols.Length / (double)perPage);
                if (pages > 10)
                    // Needs more than 10 subs to subscribe to all symbols, just go with ticker
                    continue;

                var exchangeSubs = new List<UpdateSubscription>();
                _logger.LogDebug("TickerPublishService starting batched ticker for {Exchange} in {Pages} batches", tickerClient.Exchange, pages);
                var success = true;
                for (var i = 0; i < pages; i++)
                {
                    var symbols = exchangeSymbols[offset..(offset + perPage)];
                    var subResult = await SubscribeToTickersAsync(tickerClient, symbols);
                    if (!subResult)
                    {
                        await Task.WhenAll(exchangeSubs.Select(x => x.CloseAsync()));
                        break;
                    }

                    exchangeSubs.Add(subResult.Data);
                }

                if (success)
                    subbedExchanges.Add(tickerClient.Exchange);
            }

            return subbedExchanges;
        }

        private async Task<ExchangeResult<UpdateSubscription>> SubscribeToTickersAsync(ITickerSocketClient tickerClient, SharedSpotSymbol[] symbols)
        {
            var subResult = await tickerClient.SubscribeToTickerUpdatesAsync(new SubscribeTickerRequest(symbols.Select(x => x.SharedSymbol)), ProcessUpdate, _stoppingToken);
            if (!subResult)
            {
                _logger.LogDebug("TickerPublishService batch ticker for {Exchange} failed", tickerClient.Exchange);
                return subResult;
            }

            subResult.Data.ResubscribingFailed += async (x) =>
            {
                if (x.ErrorType == ErrorType.UnknownSymbol
                || x.ErrorType == ErrorType.InvalidParameter)
                {
                    // This probably means the symbol is no longer online
                    // Unsubscribe and resub with the symbols which are correct 
                    _logger.LogError($"TickerPublishService resubscribing failed with error {x.ErrorType}, checking valid symbols and resubscribing");
                    await subResult.Data.CloseAsync();
                    var validSymbols = GetValidSymbols(tickerClient.Exchange, symbols);

                    // TODO: if this fails there is no backup to get the tickers up and running again
                    var result = await SubscribeToTickersAsync(tickerClient, validSymbols);
                    if (!result)
                        _logger.LogError($"TickerPublishService resubscribing symbols failed; dropped symbols: [{string.Join(", ", symbols.Select(x => x.Name).Except(validSymbols.Select(x => x.Name)))}]");
                    else
                        _logger.LogError($"TickerPublishService resubscribing symbols succeeded; dropped symbols: [{string.Join(", ", symbols.Select(x => x.Name).Except(validSymbols.Select(x => x.Name)))}]");

                }
            };

            return subResult;
        }

        private SharedSpotSymbol[] GetValidSymbols(string exchange, SharedSpotSymbol[] symbols)
        {
            var exchangeSymbols = _symbols[exchange];
            return symbols.Where(x => exchangeSymbols.Any(y => x.Name == y.Name && x.Trading)).ToArray();
        }

        private void ProcessUpdate(ExchangeEvent<SharedSpotTicker> @event)
        {
            var exchangeData = new PublishItem<Ticker>(@event.Exchange);
            var data = new Dictionary<string, Ticker>();
            if (@event.Data.SharedSymbol == null)
                return;

            data.Add(@event.Exchange + @event.Data.Symbol, ParseTicker(@event.Exchange, @event.Data));
            _ = _tickerBatcher.AddAsync(data);
        }

        private void ProcessUpdate(ExchangeEvent<SharedSpotTicker[]> @event)
        {
            var exchangeData = new PublishItem<Ticker>(@event.Exchange);
            var data = new Dictionary<string, Ticker>();
            foreach (var symbol in @event.Data)
            {
                if (symbol.SharedSymbol == null)
                    continue;

                data.Add(@event.Exchange + symbol.Symbol, ParseTicker(@event.Exchange, symbol));
            }

            // TODO: Handle ticker batching properly
            _ = _tickerBatcher.AddAsync(data);
        }

        private async Task PollAsync(List<string> exchanges)
        {
            var tickersTasks = _restClient.GetSpotTickersAsyncEnumerable(new GetTickersRequest(), exchanges, _stoppingToken);
            await foreach(var result in tickersTasks)
            {
                if (!result)
                {
                    // TODO someway to publish errors
                    _logger.LogError("Failed to request tickers from exchange {Exchange}: {Error}", result.Exchange, result.Error!.ToString());
                    continue;
                }

                var exchangeData = new PublishItem<Ticker>(result.Exchange);

                var data = new Dictionary<string, Ticker>();
                foreach (var symbol in result.Data)
                {
                    if (symbol.SharedSymbol == null)
                        continue;

                    data.Add(result.Exchange + symbol.Symbol, ParseTicker(result.Exchange, symbol));
                }
                _ = _tickerBatcher.AddAsync(data);
            }
        }

        private Ticker ParseTicker(string exchange, SharedSpotTicker ticker)
        {
            return new Ticker
            {
                Exchange = exchange,
                BaseAsset = ticker.SharedSymbol!.BaseAsset,
                QuoteAsset = ticker.SharedSymbol.QuoteAsset,
                Symbol = ticker.Symbol,
                ChangePercentage = ticker.ChangePercentage,
                HighPrice = ticker.HighPrice,
                LastPrice = ticker.LastPrice,
                LowPrice = ticker.LowPrice,
                Volume = ticker.Volume,
                QuoteVolume = ticker.QuoteVolume
            };
        }

        /// <summary>
        /// Validates symbols by comparing exchange prices with Jupiter prices
        /// </summary>
        private async Task<IEnumerable<SymbolValidationResult>> ValidateSymbolsWithPriceAsync(
            string exchange, 
            SharedSpotSymbol[] symbols, 
            IEnumerable<JupiterToken> jupiterTokens)
        {
            var validationResults = new List<SymbolValidationResult>();
            var jupiterTokenDict = jupiterTokens
                .GroupBy(t => t.Symbol, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);
            
            // Process symbols in batches to avoid overwhelming the APIs
            var symbolBatches = symbols.Chunk(10);
            
            foreach (var batch in symbolBatches)
            {
                var batchTasks = batch.Select(async symbol =>
                {
                    try
                    {
                        // Check if symbol is in Jupiter verified tokens
                        if (!jupiterTokenDict.TryGetValue(symbol.BaseAsset, out var jupiterToken))
                        {
                            return new SymbolValidationResult
                            {
                                Symbol = symbol,
                                Blockchains = new[] { "other" },
                                ContractAddresses = Array.Empty<ContractAddress>(),
                                IsValidated = false
                            };
                        }

                        // Get exchange book ticker price
                        var sharedSymbol = new SharedSymbol(TradingMode.Spot, symbol.BaseAsset, symbol.QuoteAsset, symbol.Name);
                        var bookTickerRequest = new GetBookTickerRequest(sharedSymbol);
                        var bookTickerResult = await _restClient.GetBookTickerAsync(exchange, bookTickerRequest);
                        if (!bookTickerResult.Success || bookTickerResult.Data == null)
                        {
                            _logger.LogDebug("Failed to get book ticker for {Symbol} on {Exchange}", symbol.Name, exchange);
                            return new SymbolValidationResult
                            {
                                Symbol = symbol,
                                Blockchains = new[] { "other" },
                                ContractAddresses = Array.Empty<ContractAddress>(),
                                IsValidated = false
                            };
                        }

                        var exchangePrice = (bookTickerResult.Data.BestBidPrice + bookTickerResult.Data.BestAskPrice) / 2;
                        if (exchangePrice <= 0)
                        {
                            _logger.LogDebug("Invalid exchange price for {Symbol} on {Exchange}: {Price}", symbol.Name, exchange, exchangePrice);
                            return new SymbolValidationResult
                            {
                                Symbol = symbol,
                                Blockchains = new[] { "other" },
                                ContractAddresses = Array.Empty<ContractAddress>(),
                                ExchangePrice = exchangePrice,
                                IsValidated = false
                            };
                        }

                        // Get Jupiter price
                        var jupiterPrice = await _jupiterTokenService.GetTokenPriceAsync(jupiterToken.Address, _stoppingToken);
                        if (jupiterPrice == null)
                        {
                            _logger.LogDebug("Failed to get Jupiter price for {Symbol} (address: {Address})", symbol.Name, jupiterToken.Address);
                            return new SymbolValidationResult
                            {
                                Symbol = symbol,
                                Blockchains = new[] { "other" },
                                ContractAddresses = Array.Empty<ContractAddress>(),
                                ExchangePrice = exchangePrice,
                                IsValidated = false
                            };
                        }

                        var jupiterUsdPrice = jupiterPrice.UsdPrice;
                        if (jupiterUsdPrice <= 0)
                        {
                            _logger.LogDebug("Invalid Jupiter price for {Symbol}: {Price}", symbol.Name, jupiterUsdPrice);
                            return new SymbolValidationResult
                            {
                                Symbol = symbol,
                                Blockchains = new[] { "other" },
                                ContractAddresses = Array.Empty<ContractAddress>(),
                                ExchangePrice = exchangePrice,
                                JupiterPrice = jupiterUsdPrice,
                                IsValidated = false
                            };
                        }

                        // Convert exchange price to USD if needed
                        var exchangeUsdPrice = exchangePrice;
                        if (symbol.QuoteAsset != "USDT" && symbol.QuoteAsset != "USDC" && symbol.QuoteAsset != "USD")
                        {
                            // Get quote currency price in USD
                            var quoteTokenPrice = await _jupiterTokenService.GetTokenPriceAsync(symbol.QuoteAsset, _stoppingToken);
                            if (quoteTokenPrice != null && quoteTokenPrice.UsdPrice > 0)
                            {
                                exchangeUsdPrice = exchangePrice * quoteTokenPrice.UsdPrice;
                            }
                            else
                            {
                                _logger.LogDebug("Could not convert {QuoteAsset} to USD for {Symbol}, skipping price validation", symbol.QuoteAsset, symbol.Name);
                                return new SymbolValidationResult
                                {
                                    Symbol = symbol,
                                    Blockchains = new[] { "other" },
                                    ContractAddresses = Array.Empty<ContractAddress>(),
                                    ExchangePrice = exchangePrice,
                                    JupiterPrice = jupiterUsdPrice,
                                    IsValidated = false
                                };
                            }
                        }

                        // Calculate price difference percentage
                        var priceDifference = Math.Abs(exchangeUsdPrice - jupiterUsdPrice) / jupiterUsdPrice * 100;
                        
                        // Check if prices are within 10% tolerance
                        if (priceDifference <= 10.0m)
                        {
                            _logger.LogDebug("Price validation passed for {Symbol}: Exchange={ExchangePrice:F6} ({QuoteAsset}), ExchangeUSD={ExchangeUsdPrice:F6}, Jupiter={JupiterPrice:F6}, Diff={Difference:F2}%", 
                                symbol.Name, exchangePrice, symbol.QuoteAsset, exchangeUsdPrice, jupiterUsdPrice, priceDifference);
                            return new SymbolValidationResult
                            {
                                Symbol = symbol,
                                Blockchains = new[] { "solana" },
                                ContractAddresses = new[] { new ContractAddress { Network = "solana", Address = jupiterToken.Address } },
                                ExchangePrice = exchangeUsdPrice,
                                JupiterPrice = jupiterUsdPrice,
                                PriceDifference = priceDifference,
                                IsValidated = true
                            };
                        }
                        else
                        {
                            _logger.LogDebug("Price validation failed for {Symbol}: Exchange={ExchangePrice:F6} ({QuoteAsset}), ExchangeUSD={ExchangeUsdPrice:F6}, Jupiter={JupiterPrice:F6}, Diff={Difference:F2}%", 
                                symbol.Name, exchangePrice, symbol.QuoteAsset, exchangeUsdPrice, jupiterUsdPrice, priceDifference);
                            return new SymbolValidationResult
                            {
                                Symbol = symbol,
                                Blockchains = new[] { "other" },
                                ContractAddresses = Array.Empty<ContractAddress>(),
                                ExchangePrice = exchangeUsdPrice,
                                JupiterPrice = jupiterUsdPrice,
                                PriceDifference = priceDifference,
                                IsValidated = false
                            };
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error validating symbol {Symbol} on {Exchange}", symbol.Name, exchange);
                        return new SymbolValidationResult
                        {
                            Symbol = symbol,
                            Blockchains = new[] { "other" },
                            ContractAddresses = Array.Empty<ContractAddress>(),
                            IsValidated = false
                        };
                    }
                });

                var batchResults = await Task.WhenAll(batchTasks);
                validationResults.AddRange(batchResults);
                
                // Small delay between batches to avoid rate limiting
                await Task.Delay(100, _stoppingToken);
            }

            return validationResults;
        }
    }
}
