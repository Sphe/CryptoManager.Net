using CryptoExchange.Net.SharedApis;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace CryptoManager.Net.Database.Models
{
    public class UserOrder
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = string.Empty;
        
        [BsonElement("exchange")]
        public string Exchange { get; set; } = string.Empty;
        
        [BsonElement("symbolId")]
        public string SymbolId { get; set; } = string.Empty;
        
        [BsonElement("orderId")]
        public string OrderId { get; set; } = string.Empty;
        
        [BsonElement("orderSide")]
        public SharedOrderSide OrderSide { get; set; }
        
        [BsonElement("orderType")]
        public SharedOrderType OrderType { get; set; }
        
        [BsonElement("status")]
        public SharedOrderStatus Status { get; set; }
        
        [BsonElement("orderPrice")]
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal? OrderPrice { get; set; }
        
        [BsonElement("orderQuantityBase")]
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal? OrderQuantityBase { get; set; }
        
        [BsonElement("orderQuantityQuote")]
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal? OrderQuantityQuote { get; set; }
        
        [BsonElement("quantityFilledBase")]
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal? QuantityFilledBase { get; set; }
        
        [BsonElement("quantityFilledQuote")]
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal? QuantityFilledQuote { get; set; }
        
        [BsonElement("averagePrice")]
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal? AveragePrice { get; set; }
        
        [BsonElement("createTime")]
        public DateTime CreateTime { get; set; }        
        
        [BsonElement("updateTime")]
        public DateTime UpdateTime { get; set; }
        
        [BsonElement("userId")]
        public string UserId { get; set; } = string.Empty;
        
        [BsonIgnore]
        public User User { get; set; } = default!;
    }
}
