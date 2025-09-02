namespace CryptoManager.Net.Models.Response
{
    public record ApiApiKey
    {
        public int Id { get; set; }
        public string Exchange { get; set; } = string.Empty;
        public string Environment { get; set; } = string.Empty;
        public string Key { get; set; } = string.Empty;
        public bool Invalid { get; set; }
    }
}
