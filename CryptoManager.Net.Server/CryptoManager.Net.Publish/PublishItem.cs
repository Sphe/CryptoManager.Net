namespace CryptoManager.Net.Publish
{
    public record PublishItem<T>
    {
        public string? Exchange { get; set; }
        public IEnumerable<T> Data { get; set; } = default!;
        public DateTime Timestamp { get; set; }

        public PublishItem(string? exchange = null)
        {
            Exchange = exchange;
            Timestamp = DateTime.UtcNow;
        }
    }
}
