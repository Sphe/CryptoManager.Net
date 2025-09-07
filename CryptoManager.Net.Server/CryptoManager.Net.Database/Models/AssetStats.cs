using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace CryptoManager.Net.Database.Models
{
    public class AssetStats
    {
        [BsonId]
        [BsonElement("_id")]
        public string Asset { get; set; } = string.Empty;

        [BsonElement("blockchains")]
        public string[] Blockchains { get; set; } = new[] { "other" };

        [BsonElement("contractAddresses")]
        public ContractAddress[] ContractAddresses { get; set; } = Array.Empty<ContractAddress>();

        [BsonElement("assetType")]
        public AssetType AssetType { get; set; }

        [BsonElement("value")]
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal? Value { get; set; }

        [BsonElement("volume")]
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal Volume { get; set; }

        [BsonElement("changePercentage")]
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal? ChangePercentage { get; set; }

        [BsonElement("jupiterPrice")]
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal? JupiterPrice { get; set; }

        [BsonElement("exchangePrice")]
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal? ExchangePrice { get; set; }

        [BsonElement("priceDifference")]
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal? PriceDifference { get; set; }

        [BsonElement("updateTime")]
        public DateTime UpdateTime { get; set; }

        [BsonElement("exchangeCount")]
        public int ExchangeCount { get; set; }

        [BsonElement("exchanges")]
        public string[] Exchanges { get; set; } = Array.Empty<string>();
    }
}