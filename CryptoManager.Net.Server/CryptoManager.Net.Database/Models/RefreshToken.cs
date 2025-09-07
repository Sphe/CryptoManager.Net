using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace CryptoManager.Net.Database.Models
{
    public class RefreshToken
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = string.Empty;
        
        [BsonElement("userId")]
        public string UserId { get; set; } = string.Empty;
        
        [BsonElement("token")]
        public string Token { get; set; } = string.Empty;
        
        [BsonElement("expireTime")]
        public DateTime ExpireTime { get; set; }
    }
}
