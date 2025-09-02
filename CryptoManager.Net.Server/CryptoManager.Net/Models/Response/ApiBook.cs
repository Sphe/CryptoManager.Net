using CryptoExchange.Net.Interfaces;

namespace CryptoManager.Net.Models.Response
{
    public class ApiBook
    {
        public ISymbolOrderBookEntry[] Asks { get; set; } = [];
        public ISymbolOrderBookEntry[] Bids { get; set; } = [];
    }
}
