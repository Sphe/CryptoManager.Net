namespace CryptoManager.Net.Database.Models
{
    public class UserQuickViewConfiguration
    {
        public int Id { get; set; }
        public string SymbolId { get; set; } = string.Empty;

        public int UserId { get; set; }
        public User User { get; set; } = default!;
    }
}
