using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace CryptoManager.Net.Database.Models
{
    public class UserBalance
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = string.Empty;
        
        [BsonElement("exchange")]
        public string Exchange { get; set; } = string.Empty;
        
        [BsonElement("asset")]
        public string Asset { get; set; } = string.Empty;
        
        [BsonElement("available")]
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal Available { get; set; }
        
        [BsonElement("total")]
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal Total { get; set; }

        [BsonElement("userId")]
        public string UserId { get; set; } = string.Empty;
        
        [BsonIgnore]
        public User User { get; set; } = default!;
    }
}
