using CryptoExchange.Net.Objects;

namespace CryptoManager.Net.UI.Models.ApiModels.Response
{
    public record ApiExchangeDetails
    {
        public string Exchange { get; set; } = string.Empty;
        public int Symbols { get; set; }
        public decimal UsdVolume { get; set; }
        public ExchangeType Type { get; set; }
        public string Url { get; set; } = string.Empty;
        public string LogoUrl { get; set; } = string.Empty;
    }
}
