namespace CryptoManager.Net.Models.Response
{
    public class ApiAssetBalance
    {
        public string Name { get; set; } = string.Empty;
        public decimal Value { get; set; }
        public decimal Price { get; set; }
    }
}
