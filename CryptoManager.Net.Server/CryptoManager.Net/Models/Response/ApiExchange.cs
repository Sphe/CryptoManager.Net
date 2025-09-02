namespace CryptoManager.Net.Models.Response
{
    public record ApiExchange
    {
        public string Exchange { get; set; } = string.Empty;
        public int Symbols { get; set; }
        public decimal UsdVolume { get; set; }
    }
}
