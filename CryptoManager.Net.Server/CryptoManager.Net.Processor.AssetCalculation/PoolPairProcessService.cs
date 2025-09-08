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
    public class PoolPairProcessService : IBackgroundService
    {
        private CancellationToken _stoppingToken = default;
        private readonly ILogger<PoolPairProcessService> _logger;
        private readonly IMongoDatabaseFactory _mongoDatabaseFactory;
        private readonly IJupiterTokenService _jupiterTokenService;
        private readonly IProcessInput<PendingPoolPairCalculation> _poolPairCalculationInput;
        private DateTime _lastLog;
        
        // Rate limiting for Jupiter API calls
        private readonly Queue<DateTime> _jupiterCallTimes = new();
        private readonly int _maxCallsPerMinute = 10;
        private readonly TimeSpan _rateLimitWindow = TimeSpan.FromMinutes(1);

        public PoolPairProcessService(
            ILogger<PoolPairProcessService> logger,
            IMongoDatabaseFactory mongoDatabaseFactory,
            IJupiterTokenService jupiterTokenService,
            IProcessInput<PendingPoolPairCalculation> poolPairCalculationInput)
        {
            _logger = logger;
            _mongoDatabaseFactory = mongoDatabaseFactory;
            _jupiterTokenService = jupiterTokenService;
            _poolPairCalculationInput = poolPairCalculationInput;
        }

        private async Task<bool> WaitForRateLimitAsync()
        {
            var now = DateTime.UtcNow;
            
            // Remove old call times outside the rate limit window
            while (_jupiterCallTimes.Count > 0 && now - _jupiterCallTimes.Peek() > _rateLimitWindow)
            {
                _jupiterCallTimes.Dequeue();
            }
            
            // Check if we're at the rate limit
            if (_jupiterCallTimes.Count >= _maxCallsPerMinute)
            {
                var oldestCall = _jupiterCallTimes.Peek();
                var waitTime = _rateLimitWindow - (now - oldestCall);
                
                if (waitTime > TimeSpan.Zero)
                {
                    _logger.LogInformation($"Rate limit reached. Waiting {waitTime.TotalSeconds:F1} seconds before next Jupiter API call");
                    await Task.Delay(waitTime, _stoppingToken);
                    return await WaitForRateLimitAsync(); // Recursive call to recheck after waiting
                }
            }
            
            // Record this call time
            _jupiterCallTimes.Enqueue(now);
            return true;
        }

        public async Task ExecuteAsync(CancellationToken ct)
        {
            _stoppingToken = ct;

            _logger.LogDebug("Starting PoolPairProcessService");
            while (!_stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessAsync();
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in PoolPairProcessService");
                    await Task.Delay(TimeSpan.FromSeconds(5), _stoppingToken);
                }
            }

            _logger.LogDebug("PoolPairProcessService stopped");
        }

        private async Task ProcessAsync()
        {
            var context = _mongoDatabaseFactory.CreateContext();
            var update = await _poolPairCalculationInput.ReadAsync(_stoppingToken);

            if (update == null || !update.Data.Any())
                return;

            var sw = Stopwatch.StartNew();

            try
            {
                _logger.LogDebug("Processing {Count} pool pair calculations", update.Data.Count());

                var poolPairs = new List<PoolPairs>();

                // Process each pending calculation
                foreach (var pendingCalculation in update.Data)
                {
                    try
                    {
                        // Wait for rate limit before making Jupiter API call
                        await WaitForRateLimitAsync();
                        
                        // Get swap quote from Jupiter
                        var quote = await _jupiterTokenService.GetSwapQuoteAsync(
                            pendingCalculation.InputMint,
                            pendingCalculation.OutputMint,
                            pendingCalculation.Amount,
                            cancellationToken: _stoppingToken);

                        if (quote != null)
                        {
                            var poolPair = new PoolPairs
                            {
                                Id = $"{pendingCalculation.Asset}-{pendingCalculation.ContractAddress}",
                                Asset = pendingCalculation.Asset,
                                ContractAddress = pendingCalculation.ContractAddress,
                                RoutePlan = quote.RoutePlan.Select(rp => new CryptoManager.Net.Database.Models.JupiterRoutePlan
                                {
                                    SwapInfo = new CryptoManager.Net.Database.Models.JupiterSwapInfo
                                    {
                                        AmmKey = rp.SwapInfo.AmmKey,
                                        Label = rp.SwapInfo.Label,
                                        InputMint = rp.SwapInfo.InputMint,
                                        OutputMint = rp.SwapInfo.OutputMint,
                                        InAmount = rp.SwapInfo.InAmount,
                                        OutAmount = rp.SwapInfo.OutAmount,
                                        FeeAmount = rp.SwapInfo.FeeAmount,
                                        FeeMint = rp.SwapInfo.FeeMint
                                    },
                                    Percent = rp.Percent,
                                    Bps = rp.Bps
                                }).ToArray(),
                                InputMint = quote.InputMint,
                                OutputMint = quote.OutputMint,
                                InAmount = quote.InAmount,
                                OutAmount = quote.OutAmount,
                                PriceImpactPct = quote.PriceImpactPct,
                                SwapUsdValue = quote.SwapUsdValue,
                                RouteCount = quote.RoutePlan.Length,
                                UpdateTime = DateTime.UtcNow,
                                LastProcessed = DateTime.UtcNow
                            };

                            poolPairs.Add(poolPair);
                        }
                        else
                        {
                            _logger.LogWarning("No swap quote found for {Asset} ({ContractAddress})", 
                                pendingCalculation.Asset, pendingCalculation.ContractAddress);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing pool pair calculation for {Asset} ({ContractAddress})", 
                            pendingCalculation.Asset, pendingCalculation.ContractAddress);
                    }
                }

                // Bulk upsert pool pairs
                if (poolPairs.Any())
                {
                    var bulkOps = new List<WriteModel<PoolPairs>>();
                    foreach (var poolPair in poolPairs)
                    {
                        var filter = Builders<PoolPairs>.Filter.Eq(x => x.Id, poolPair.Id);
                        var updateDefinition = Builders<PoolPairs>.Update
                            .Set(x => x.Asset, poolPair.Asset)
                            .Set(x => x.ContractAddress, poolPair.ContractAddress)
                            .Set(x => x.RoutePlan, poolPair.RoutePlan)
                            .Set(x => x.InputMint, poolPair.InputMint)
                            .Set(x => x.OutputMint, poolPair.OutputMint)
                            .Set(x => x.InAmount, poolPair.InAmount)
                            .Set(x => x.OutAmount, poolPair.OutAmount)
                            .Set(x => x.PriceImpactPct, poolPair.PriceImpactPct)
                            .Set(x => x.SwapUsdValue, poolPair.SwapUsdValue)
                            .Set(x => x.RouteCount, poolPair.RouteCount)
                            .Set(x => x.UpdateTime, poolPair.UpdateTime)
                            .Set(x => x.LastProcessed, poolPair.LastProcessed);

                        bulkOps.Add(new UpdateOneModel<PoolPairs>(filter, updateDefinition) { IsUpsert = true });
                    }

                    await context.PoolPairs.BulkWriteAsync(bulkOps);
                }

                sw.Stop();

                if (DateTime.UtcNow - _lastLog > TimeSpan.FromMinutes(1))
                {
                    _lastLog = DateTime.UtcNow;
                    _logger.LogInformation($"Pool pair processing done in {sw.ElapsedMilliseconds}ms for {poolPairs.Count}/{update.Data.Count()} calculations");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in pool pair processing");
            }
        }
    }
}
