using CryptoManager.Net.Database.Models;
using MongoDB.Driver;

namespace CryptoManager.Net.Database
{
    public class MongoTrackerContext
    {
        private readonly IMongoDatabase _database;

        public MongoTrackerContext(IMongoDatabase database)
        {
            _database = database;
        }

        public IMongoCollection<ExchangeSymbol> Symbols => _database.GetCollection<ExchangeSymbol>("symbols");
        public IMongoCollection<ExchangeSymbol> ExchangeSymbols => _database.GetCollection<ExchangeSymbol>("symbols");
        public IMongoCollection<FiatPrice> FiatPrices => _database.GetCollection<FiatPrice>("fiatPrices");
        public IMongoCollection<ExchangeAssetStats> ExchangeAssetStats => _database.GetCollection<ExchangeAssetStats>("exchangeAssetStats");
        public IMongoCollection<AssetStats> AssetStats => _database.GetCollection<AssetStats>("assetStats");
        public IMongoCollection<PoolPairs> PoolPairs => _database.GetCollection<PoolPairs>("poolPairs");
        public IMongoCollection<InventoryPoolPairs> InventoryPoolPairs => _database.GetCollection<InventoryPoolPairs>("inventoryPoolPairs");
        public IMongoCollection<User> Users => _database.GetCollection<User>("users");
        public IMongoCollection<UserQuickViewConfiguration> UserQuickViewConfigurations => _database.GetCollection<UserQuickViewConfiguration>("userQuickViewConfigurations");
        public IMongoCollection<RefreshToken> RefreshTokens => _database.GetCollection<RefreshToken>("refreshTokens");
        public IMongoCollection<UserApiKey> UserApiKeys => _database.GetCollection<UserApiKey>("userApiKeys");
        public IMongoCollection<UserUpdate> UserUpdates => _database.GetCollection<UserUpdate>("userUpdates");
        public IMongoCollection<UserBalance> UserBalances => _database.GetCollection<UserBalance>("userBalances");
        public IMongoCollection<UserExternalBalance> UserExternalBalances => _database.GetCollection<UserExternalBalance>("userExternalBalances");
        public IMongoCollection<UserOrder> UserOrders => _database.GetCollection<UserOrder>("userOrders");
        public IMongoCollection<UserTrade> UserTrades => _database.GetCollection<UserTrade>("userTrades");
        public IMongoCollection<UserValuation> UserValuations => _database.GetCollection<UserValuation>("userValuations");

        public async Task CreateIndexesAsync()
        {
            // Create indexes for ExchangeSymbol
            await Symbols.Indexes.CreateOneAsync(
                new CreateIndexModel<ExchangeSymbol>(
                    Builders<ExchangeSymbol>.IndexKeys.Ascending(x => x.UpdateTime).Ascending(x => x.Exchange).Ascending(x => x.BaseAsset)
                )
            );

            // Create indexes for ExchangeAssetStats
            await ExchangeAssetStats.Indexes.CreateOneAsync(
                new CreateIndexModel<ExchangeAssetStats>(
                    Builders<ExchangeAssetStats>.IndexKeys.Ascending(x => x.Exchange).Ascending(x => x.Value).Ascending(x => x.Asset)
                )
            );

            await ExchangeAssetStats.Indexes.CreateOneAsync(
                new CreateIndexModel<ExchangeAssetStats>(
                    Builders<ExchangeAssetStats>.IndexKeys.Ascending(x => x.AssetType).Ascending(x => x.Value).Ascending(x => x.Volume).Ascending(x => x.ChangePercentage).Ascending(x => x.Asset)
                )
            );

            // Create indexes for AssetStats (unique symbols)
            await AssetStats.Indexes.CreateOneAsync(
                new CreateIndexModel<AssetStats>(
                    Builders<AssetStats>.IndexKeys.Ascending(x => x.AssetType).Ascending(x => x.Value).Ascending(x => x.Volume).Ascending(x => x.ChangePercentage)
                )
            );

            await AssetStats.Indexes.CreateOneAsync(
                new CreateIndexModel<AssetStats>(
                    Builders<AssetStats>.IndexKeys.Ascending(x => x.ExchangeCount).Ascending(x => x.Value)
                )
            );

            await AssetStats.Indexes.CreateOneAsync(
                new CreateIndexModel<AssetStats>(
                    Builders<AssetStats>.IndexKeys.Ascending(x => x.Blockchains).Ascending(x => x.Value)
                )
            );

            await AssetStats.Indexes.CreateOneAsync(
                new CreateIndexModel<AssetStats>(
                    Builders<AssetStats>.IndexKeys.Ascending(x => x.ContractAddresses)
                )
            );

            // Create indexes for PoolPairs
            await PoolPairs.Indexes.CreateOneAsync(
                new CreateIndexModel<PoolPairs>(
                    Builders<PoolPairs>.IndexKeys.Ascending(x => x.Asset)
                )
            );

            await PoolPairs.Indexes.CreateOneAsync(
                new CreateIndexModel<PoolPairs>(
                    Builders<PoolPairs>.IndexKeys.Ascending(x => x.ContractAddress)
                )
            );

            await PoolPairs.Indexes.CreateOneAsync(
                new CreateIndexModel<PoolPairs>(
                    Builders<PoolPairs>.IndexKeys.Ascending(x => x.UpdateTime)
                )
            );

            // Create indexes for UserBalance
            await UserBalances.Indexes.CreateOneAsync(
                new CreateIndexModel<UserBalance>(
                    Builders<UserBalance>.IndexKeys.Ascending(x => x.UserId)
                )
            );

            await UserBalances.Indexes.CreateOneAsync(
                new CreateIndexModel<UserBalance>(
                    Builders<UserBalance>.IndexKeys.Ascending(x => x.Exchange).Ascending(x => x.Asset)
                )
            );

            // Create indexes for UserOrder
            await UserOrders.Indexes.CreateOneAsync(
                new CreateIndexModel<UserOrder>(
                    Builders<UserOrder>.IndexKeys.Ascending(x => x.UserId)
                )
            );

            await UserOrders.Indexes.CreateOneAsync(
                new CreateIndexModel<UserOrder>(
                    Builders<UserOrder>.IndexKeys.Ascending(x => x.Exchange).Ascending(x => x.SymbolId)
                )
            );

            // Create indexes for UserTrade
            await UserTrades.Indexes.CreateOneAsync(
                new CreateIndexModel<UserTrade>(
                    Builders<UserTrade>.IndexKeys.Ascending(x => x.UserId)
                )
            );

            await UserTrades.Indexes.CreateOneAsync(
                new CreateIndexModel<UserTrade>(
                    Builders<UserTrade>.IndexKeys.Ascending(x => x.Exchange).Ascending(x => x.SymbolId)
                )
            );

            // Create indexes for UserApiKey
            await UserApiKeys.Indexes.CreateOneAsync(
                new CreateIndexModel<UserApiKey>(
                    Builders<UserApiKey>.IndexKeys.Ascending(x => x.UserId)
                )
            );

            // Create indexes for UserExternalBalance
            await UserExternalBalances.Indexes.CreateOneAsync(
                new CreateIndexModel<UserExternalBalance>(
                    Builders<UserExternalBalance>.IndexKeys.Ascending(x => x.UserId)
                )
            );

            // Create unique index for InventoryPoolPairs on contractAddress
            await InventoryPoolPairs.Indexes.CreateOneAsync(
                new CreateIndexModel<InventoryPoolPairs>(
                    Builders<InventoryPoolPairs>.IndexKeys.Ascending(x => x.ContractAddress),
                    new CreateIndexOptions { Unique = true }
                )
            );
        }
    }
}
