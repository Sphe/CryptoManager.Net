using CryptoExchange.Net.SharedApis;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace CryptoManager.Net.Database.Models
{
    public class UserTrade
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = string.Empty;
        
        [BsonElement("exchange")]
        public string Exchange { get; set; } = string.Empty;
        
        [BsonElement("symbolId")]
        public string SymbolId { get; set; } = string.Empty;
        
        [BsonElement("tradeId")]
        public string TradeId { get; set; } = string.Empty;
        
        [BsonElement("price")]
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal? Price { get; set; }
        
        [BsonElement("quantity")]
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal? Quantity { get; set; }
        
        [BsonElement("feeAsset")]
        public string? FeeAsset { get; set; }
        
        [BsonElement("fee")]
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal? Fee { get; set; }
        
        [BsonElement("role")]
        public SharedRole? Role { get; set; }
        
        [BsonElement("side")]
        public SharedOrderSide? Side { get; set; }
        
        [BsonElement("createTime")]
        public DateTime? CreateTime { get; set; }

        [BsonElement("orderId")]
        public string OrderId { get; set; } = string.Empty;
        
        [BsonElement("userId")]
        public string UserId { get; set; } = string.Empty;
        
        [BsonIgnore]
        public User User { get; set; } = default!;
    }
}
