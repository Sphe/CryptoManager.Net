namespace CryptoManager.Net.Database.Models
{
    public class UserApiKey
    {
        public int Id { get; set; }
        public string? Environment { get; set; }
        public string Exchange { get; set; } = string.Empty;
        public string Key { get; set; } = string.Empty;
        public string Secret { get; set; } = string.Empty;
        public string? Pass { get; set; }
        public bool Invalid { get; set; }

        public int UserId { get; set; }
        public virtual User User { get; set; } = default!;
    }
}
