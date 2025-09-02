using Microsoft.EntityFrameworkCore;

namespace CryptoManager.Net.Database.Models
{
    public class UserBalance
    {
        public string Id { get; set; } = string.Empty;
        public string Exchange { get; set; } = string.Empty;
        public string Asset { get; set; } = string.Empty;
        [Precision(28, 8)]
        public decimal Available { get; set; }
        [Precision(28, 8)]
        public decimal Total { get; set; }

        public int UserId { get; set; }
        public virtual User User { get; set; } = default!;
    }
}
