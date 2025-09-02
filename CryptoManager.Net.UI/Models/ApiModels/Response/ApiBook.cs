using CryptoExchange.Net.Interfaces;
using CryptoExchange.Net.SharedApis;

namespace CryptoManager.Net.Models.Response
{
    public class ApiBook
    {
        public BookEntry[] Asks { get; set; } = [];
        public BookEntry[] Bids { get; set; } = [];
    }

    public class BookEntry
    {
        public decimal Quantity { get; set; }
        public decimal Price { get; set; }
    }
}
