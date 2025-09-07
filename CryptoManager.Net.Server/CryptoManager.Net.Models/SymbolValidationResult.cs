using CryptoExchange.Net.SharedApis;
using CryptoManager.Net.Database.Models;

namespace CryptoManager.Net.Models
{
    public class SymbolValidationResult
    {
        public SharedSpotSymbol Symbol { get; set; } = null!;
        public string[] Blockchains { get; set; } = new[] { "other" };
        public ContractAddress[] ContractAddresses { get; set; } = Array.Empty<ContractAddress>();
        public decimal? JupiterPrice { get; set; }
        public decimal? ExchangePrice { get; set; }
        public decimal? PriceDifference { get; set; }
        public bool IsValidated { get; set; }
    }
}
