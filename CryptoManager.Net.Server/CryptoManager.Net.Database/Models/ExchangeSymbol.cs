using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace CryptoManager.Net.Database.Models
{
    public class ExchangeSymbol
    {
        [BsonId]
        [BsonElement("_id")]
        public string Id { get; set; } = string.Empty;
        
        [BsonElement("baseAssetExchangeId")]
        public string BaseAssetExchangeId { get; set; } = string.Empty;
        
        [BsonElement("quoteAssetExchangeId")]
        public string QuoteAssetExchangeId { get; set; } = string.Empty;
        
        [BsonElement("exchange")]
        public string Exchange { get; set; } = string.Empty;
        
        [BsonElement("name")]
        public string Name { get; set; } = string.Empty;
        
        [BsonElement("baseAsset")]
        public string BaseAsset { get; set; } = string.Empty;
        
        [BsonElement("quoteAsset")]
        public string QuoteAsset { get; set; } = string.Empty;
        
        [BsonElement("lastPrice")]
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal? LastPrice { get; set; }
        
        [BsonElement("highPrice")]
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal? HighPrice { get; set; }
        
        [BsonElement("lowPrice")]
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal? LowPrice { get; set; }
        
        [BsonElement("volume")]
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal? Volume { get; set; }
        
        [BsonElement("quoteVolume")]
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal? QuoteVolume { get; set; }
        
        [BsonElement("usdVolume")]
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal? UsdVolume { get; set; }
        
        [BsonElement("changePercentage")]
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal? ChangePercentage { get; set; }

        [BsonElement("minTradeQuantity")]
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal? MinTradeQuantity { get; set; }
        
        [BsonElement("minNotionalValue")]
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal? MinNotionalValue { get; set; }
        
        [BsonElement("quantityStep")]
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal? QuantityStep { get; set; }
        
        [BsonElement("priceStep")]
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal? PriceStep { get; set; }
        
        [BsonElement("quantityDecimals")]
        public int? QuantityDecimals { get; set; }
        
        [BsonElement("priceDecimals")]
        public int? PriceDecimals { get; set; }
        
        [BsonElement("priceSignificantFigures")]
        public int? PriceSignificantFigures { get; set; }

        [BsonElement("quoteAssetType")]
        public AssetType QuoteAssetType { get; set; }
        
        [BsonElement("baseAssetType")]
        public AssetType BaseAssetType { get; set; }

        [BsonElement("enabled")]
        public bool? Enabled { get; set; }

        [BsonElement("updateTime")]
        public DateTime UpdateTime { get; set; }
        
        [BsonElement("deleteTime")]
        public DateTime? DeleteTime { get; set; }
    }
}
