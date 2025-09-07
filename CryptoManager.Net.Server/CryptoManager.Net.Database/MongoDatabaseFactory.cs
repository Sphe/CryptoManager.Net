using MongoDB.Driver;

namespace CryptoManager.Net.Database
{
    public interface IMongoDatabaseFactory
    {
        MongoTrackerContext CreateContext();
        IMongoDatabase GetDatabase();
    }

    public class MongoDatabaseFactory : IMongoDatabaseFactory
    {
        private readonly IMongoDatabase _database;

        public MongoDatabaseFactory(IMongoDatabase database)
        {
            _database = database;
        }

        public MongoTrackerContext CreateContext()
        {
            return new MongoTrackerContext(_database);
        }

        public IMongoDatabase GetDatabase()
        {
            return _database;
        }
    }
}
