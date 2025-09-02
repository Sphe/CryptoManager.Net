namespace CryptoManager.Net.UI.Models.ApiModels.Response
{
    public class ApiUser
    {
        public int Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string[] AuthenticatedExchanges { get; set; } = [];
        public string Jwt { get; set; } = string.Empty;

        public bool ExchangeAuthenticated(string? exchange) => exchange == null ? false : AuthenticatedExchanges.Contains(exchange);

    }
}
