using CryptoManager.Net.Database;
using CryptoManager.Net.Publish;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace CryptoManager.Net.Processor.FiatPrices
{
    public class FiatPricesProcessService : IBackgroundService
    {
        private CancellationToken _stoppingToken = default;
        private readonly ILogger _logger;
        private readonly IProcessInput<Models.FiatPrice> _processInput;
        private readonly IMongoDatabaseFactory _mongoDatabaseFactory;

        public FiatPricesProcessService(
            ILogger<FiatPricesProcessService> logger,
            IProcessInput<Models.FiatPrice> publishOutput,
            IMongoDatabaseFactory mongoDatabaseFactory)
        {
            _logger = logger;
            _processInput = publishOutput;
            _mongoDatabaseFactory = mongoDatabaseFactory;
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

        private async Task ProcessAsync(PublishItem<Models.FiatPrice> update)
        {
            var context = _mongoDatabaseFactory.CreateContext();

            var fiatPrices = new List<Database.Models.FiatPrice>();
            foreach (var item in update.Data)
            {
                var symbol = new Database.Models.FiatPrice
                {
                    Id = item.Currency,
                    Price = item.Price,
                    UpdateTime = DateTime.UtcNow
                };

                fiatPrices.Add(symbol);
            }

            try
            {
                var bulkOps = new List<WriteModel<Database.Models.FiatPrice>>();
                foreach (var fiatPrice in fiatPrices)
                {
                    var filter = Builders<Database.Models.FiatPrice>.Filter.Eq(x => x.Id, fiatPrice.Id);
                    var updateDefinition = Builders<Database.Models.FiatPrice>.Update
                        .Set(x => x.Price, fiatPrice.Price)
                        .Set(x => x.UpdateTime, fiatPrice.UpdateTime);

                    bulkOps.Add(new UpdateOneModel<Database.Models.FiatPrice>(filter, updateDefinition) { IsUpsert = true });
                }

                if (bulkOps.Any())
                    await context.FiatPrices.BulkWriteAsync(bulkOps);
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "Failed to process Fiat prices update");
            }
        }
    }
}
