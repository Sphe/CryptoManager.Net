namespace CryptoManager.Net.Models.Response
{
    public class ApiExchangeSymbols
    {
        public string Exchange { get; set; } = string.Empty;
        public Dictionary<string, List<string>> Symbols { get; set; } = new Dictionary<string, List<string>>();
    }
}
