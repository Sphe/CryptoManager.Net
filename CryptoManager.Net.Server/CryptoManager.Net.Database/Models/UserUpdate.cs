using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace CryptoManager.Net.Database.Models
{
    public class UserUpdate
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = string.Empty;
        
        [BsonElement("type")]
        public UserUpdateType Type { get; set; }
        
        [BsonElement("lastUpdate")]
        public DateTime LastUpdate { get; set; }

        [BsonElement("userId")]
        public string UserId { get; set; } = string.Empty;
        
        [BsonIgnore]
        public User User { get; set; } = default!;
    }


    public enum UserUpdateType
    {
        OpenOrders,
        ClosedOrders,
        Balances,
        UserTrades
    }
}
