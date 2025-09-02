namespace CryptoManager.Net.Models.Response
{
    public class ApiUser
    {
        public int Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string[] AuthenticatedExchanges { get; set; } = [];
        public string? Jwt { get; set; }
    }
}
