using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace CryptoManager.Net.Database.Models
{
    public class FiatPrice
    {
        [BsonId]
        [BsonElement("_id")]
        public string Id { get; set; } = string.Empty;
        
        [BsonElement("price")]
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal Price { get; set; }
        
        [BsonElement("updateTime")]
        public DateTime UpdateTime { get; set; }
    }
}
