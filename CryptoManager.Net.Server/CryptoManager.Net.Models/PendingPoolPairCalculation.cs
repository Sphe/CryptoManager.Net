namespace CryptoManager.Net.Models
{
    public class PendingPoolPairCalculation
    {
        public string Asset { get; set; } = string.Empty;
        public string ContractAddress { get; set; } = string.Empty;
        public string InputMint { get; set; } = string.Empty; // WSOL address
        public string OutputMint { get; set; } = string.Empty; // Asset contract address
        public string Amount { get; set; } = string.Empty; // Amount to swap (e.g., "1000000" for 1 WSOL)
    }
}
