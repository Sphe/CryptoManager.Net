namespace CryptoManager.Net.Models.Response
{
    public class ApiTicker
    {
        public string SymbolId { get; set; } = string.Empty;
        public decimal? LastPrice { get; set; }
        public decimal? HighPrice { get; set; }
        public decimal? LowPrice { get; set; }
        public decimal Volume { get; set; }
        public decimal? QuoteVolume { get; set; }
        public decimal? ChangePercentage { get; set; }
    }
}
