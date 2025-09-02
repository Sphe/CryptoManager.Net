using Microsoft.EntityFrameworkCore;

namespace CryptoManager.Net.Database.Models
{
    [Index(nameof(UserId), nameof(Type), IsUnique = true)]
    public class UserUpdate
    {
        public int Id { get; set; }
        public UserUpdateType Type { get; set; }
        public DateTime LastUpdate { get; set; }

        public int UserId { get; set; }
        public virtual User User { get; set; } = default!;
    }


    public enum UserUpdateType
    {
        OpenOrders,
        ClosedOrders,
        Balances,
        UserTrades
    }
}
