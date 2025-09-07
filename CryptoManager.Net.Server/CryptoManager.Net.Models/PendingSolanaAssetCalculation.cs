using CryptoManager.Net.Database.Models;

namespace CryptoManager.Net.Models
{
    public class PendingSolanaAssetCalculation
    {
        public string Exchange { get; set; } = string.Empty;
        public string Asset { get; set; } = string.Empty;
        public string[] Blockchains { get; set; } = new[] { "other" };
        public ContractAddress[] ContractAddresses { get; set; } = Array.Empty<ContractAddress>();
        public decimal? JupiterPrice { get; set; }
        public decimal? ExchangePrice { get; set; }
        public decimal? PriceDifference { get; set; }
    }
}
