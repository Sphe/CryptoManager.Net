using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace CryptoManager.Net.Database.Models
{
    public class UserExternalBalance
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = string.Empty;
        
        [BsonElement("asset")]
        public string Asset { get; set; } = string.Empty;
        
        [BsonElement("total")]
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal Total { get; set; }

        [BsonElement("userId")]
        public string UserId { get; set; } = string.Empty;
        
        [BsonIgnore]
        public User User { get; set; } = default!;
    }
}
