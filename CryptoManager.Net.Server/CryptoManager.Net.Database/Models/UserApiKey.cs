using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace CryptoManager.Net.Database.Models
{
    public class UserApiKey
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = string.Empty;
        
        [BsonElement("environment")]
        public string? Environment { get; set; }
        
        [BsonElement("exchange")]
        public string Exchange { get; set; } = string.Empty;
        
        [BsonElement("key")]
        public string Key { get; set; } = string.Empty;
        
        [BsonElement("secret")]
        public string Secret { get; set; } = string.Empty;
        
        [BsonElement("pass")]
        public string? Pass { get; set; }
        
        [BsonElement("invalid")]
        public bool Invalid { get; set; }

        [BsonElement("userId")]
        public string UserId { get; set; } = string.Empty;
        
        [BsonIgnore]
        public User User { get; set; } = default!;
    }
}
