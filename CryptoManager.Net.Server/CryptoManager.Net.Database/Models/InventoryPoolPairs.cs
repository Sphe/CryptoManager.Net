using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace CryptoManager.Net.Database.Models
{
    public class InventoryPoolPairs
    {
        [BsonId]
        [BsonElement("_id")]
        public string Id { get; set; } = string.Empty;

        [BsonElement("contractAddress")]
        public string ContractAddress { get; set; } = string.Empty;

        [BsonElement("ammKey")]
        public string AmmKey { get; set; } = string.Empty;

        [BsonElement("label")]
        public string Label { get; set; } = string.Empty;

        [BsonElement("tokenA")]
        public PoolToken TokenA { get; set; } = new();

        [BsonElement("tokenB")]
        public PoolToken TokenB { get; set; } = new();

        [BsonElement("firstDiscovered")]
        public DateTime FirstDiscovered { get; set; }

        [BsonElement("lastSeen")]
        public DateTime LastSeen { get; set; }

        [BsonElement("discoveryCount")]
        public int DiscoveryCount { get; set; }
    }

    public class PoolToken
    {
        [BsonElement("mint")]
        public string Mint { get; set; } = string.Empty;

        [BsonElement("symbol")]
        public string Symbol { get; set; } = string.Empty;

        [BsonElement("name")]
        public string Name { get; set; } = string.Empty;

        [BsonElement("decimals")]
        public int Decimals { get; set; }

        [BsonElement("logoUri")]
        public string? LogoUri { get; set; }
    }
}
