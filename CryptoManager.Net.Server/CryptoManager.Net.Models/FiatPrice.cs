namespace CryptoManager.Net.Models
{
    public record FiatPrice
    {
        public string Currency { get; set; } = string.Empty;
        public decimal Price { get; set; }
    }
}
