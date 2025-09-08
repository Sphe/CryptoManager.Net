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
    public class PoolPairCalculationProcessService : IBackgroundService
    {
        private CancellationToken _stoppingToken = default;
        private readonly ILogger _logger;
        private readonly IMongoDatabaseFactory _mongoDatabaseFactory;
        private readonly IJupiterTokenService _jupiterTokenService;
        private readonly IPublishOutput<PendingPoolPairCalculation> _poolPairCalculationOutput;
        private DateTime _lastLog;
        private readonly TimeSpan _processingInterval = TimeSpan.FromMinutes(10);

        // WSOL (Wrapped SOL) mint address
        private const string WSOL_MINT = "So11111111111111111111111111111111111111112";
        // Amount to use for quote calculation (1 WSOL = 1,000,000,000 lamports)
        private const string WSOL_AMOUNT = "1000000000";

        public PoolPairCalculationProcessService(
            ILogger<PoolPairCalculationProcessService> logger,
            IMongoDatabaseFactory mongoDatabaseFactory,
            IJupiterTokenService jupiterTokenService,
            IPublishOutput<PendingPoolPairCalculation> poolPairCalculationOutput)
        {
            _logger = logger;
            _mongoDatabaseFactory = mongoDatabaseFactory;
            _jupiterTokenService = jupiterTokenService;
            _poolPairCalculationOutput = poolPairCalculationOutput;
        }

        public async Task ExecuteAsync(CancellationToken ct)
        {
            _stoppingToken = ct;

            _logger.LogDebug("Starting PoolPairCalculationProcessService");
            while (!_stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessPoolPairCalculationsAsync();
                    await Task.Delay(_processingInterval, _stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in PoolPairCalculationProcessService");
                    await Task.Delay(TimeSpan.FromMinutes(1), _stoppingToken);
                }
            }

            _logger.LogDebug("PoolPairCalculationProcessService stopped");
        }

        private async Task ProcessPoolPairCalculationsAsync()
        {
            var sw = Stopwatch.StartNew();
            var context = _mongoDatabaseFactory.CreateContext();

            try
            {
                // Get all Solana assets from AssetStats
                var solanaAssets = await context.AssetStats
                    .Find(x => x.Blockchains.Contains("solana") && x.ContractAddresses.Any())
                    .ToListAsync();

                if (!solanaAssets.Any())
                {
                    _logger.LogDebug("No Solana assets found for pool pair calculation");
                    return;
                }

                var pendingCalculations = new List<PendingPoolPairCalculation>();

                foreach (var asset in solanaAssets)
                {
                    // Get the Solana contract address
                    var solanaContract = asset.ContractAddresses.FirstOrDefault(ca => ca.Network == "solana");
                    if (solanaContract == null)
                        continue;

                    pendingCalculations.Add(new PendingPoolPairCalculation
                    {
                        Asset = asset.Asset,
                        ContractAddress = solanaContract.Address,
                        InputMint = WSOL_MINT,
                        OutputMint = solanaContract.Address,
                        Amount = WSOL_AMOUNT
                    });
                }

                if (pendingCalculations.Any())
                {
                    // Publish all pending calculations
                    await _poolPairCalculationOutput.PublishAsync(new PublishItem<PendingPoolPairCalculation>
                    {
                        Data = pendingCalculations
                    });

                    _logger.LogInformation("Published {Count} pool pair calculations for Solana assets", pendingCalculations.Count);
                }

                sw.Stop();

                if (DateTime.UtcNow - _lastLog > TimeSpan.FromMinutes(1))
                {
                    _lastLog = DateTime.UtcNow;
                    _logger.LogInformation("Pool pair calculation processing done in {ElapsedMs}ms for {AssetCount} Solana assets", 
                        sw.ElapsedMilliseconds, solanaAssets.Count);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in pool pair calculation processing");
            }
        }
    }
}
