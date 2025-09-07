using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace CryptoManager.Net.Database.Models
{
    public class User
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = string.Empty;
        
        [BsonElement("email")]
        public string Email { get; set; } = string.Empty;
        
        [BsonElement("password")]
        public string Password { get; set; } = string.Empty;

        [BsonElement("createTime")]
        public DateTime CreateTime { get; set; }
        
        [BsonElement("updateTime")]
        public DateTime UpdateTime { get; set; }
        
        [BsonElement("deleteTime")]
        public DateTime? DeleteTime { get; set; }

        [BsonIgnore]
        public List<UserApiKey> ApiKeys { get; set; } = new List<UserApiKey>();
        
        [BsonIgnore]
        public List<UserOrder> Orders { get; set; } = new List<UserOrder>();
    }
}
