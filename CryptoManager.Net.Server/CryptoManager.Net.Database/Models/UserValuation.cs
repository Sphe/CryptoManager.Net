using Microsoft.EntityFrameworkCore;

namespace CryptoManager.Net.Database.Models
{
    public class UserValuation
    {
        public string Id { get; set; } = string.Empty;
        [Precision(28, 8)]
        public decimal Value { get; set; }
        public DateTime Timestamp { get; set; }

        public int UserId { get; set; }
        public User User { get; set; } = default!;
    }
}
