namespace CryptoManager.Net.UI.Models
{
    public class Page<T>
    {
        public int TotalResults { get; set; }
        public IEnumerable<T> Items { get; set; } = [];

        public Page(IEnumerable<T> items, int totalResults)
        {
            Items = items;
            TotalResults = totalResults;
        }
    }
}
