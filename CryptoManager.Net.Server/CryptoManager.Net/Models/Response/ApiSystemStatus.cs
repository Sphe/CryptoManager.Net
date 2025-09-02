namespace CryptoManager.Net.Models.Response
{
    public class ApiSystemStatus
    {
        public double IncomingKbps { get; set; }

        public int Exchanges { get; set; }
        public int Symbols { get; set; }
        public int Assets { get; set; }

        public int WebsocketConnections { get; set; }
        public int UserSubscriptions { get; set; }
        public int TickerConnections { get; set; }
        public int TickerSubscriptions { get; set; }
        public int TradeConnections { get; set; }
        public int TradeSubscriptions { get; set; }
        public int OrderBookConnections { get; set; }
        public int OrderBookSubscriptions { get; set; }
    }
}
