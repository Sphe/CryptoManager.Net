namespace CryptoManager.Net.Database.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;

        public DateTime CreateTime { get; set; }
        public DateTime UpdateTime { get; set; }
        public DateTime? DeleteTime { get; set; }

        public List<UserApiKey> ApiKeys { get; set; } = new List<UserApiKey>();
        public List<UserOrder> Orders { get; set; } = new List<UserOrder>();
    }
}
