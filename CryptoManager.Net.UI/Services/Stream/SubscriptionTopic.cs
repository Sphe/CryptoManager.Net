using CryptoManager.Net.UI.Models;
using System.Net.WebSockets;
using System.Text.Json;
using System.Text;
using CryptoManager.Net.UI.Models.ApiModels.Response;

namespace CryptoManager.Net.UI.Services.Stream
{
    public enum SubscriptionTopic
    {
        Ticker,
        Trade,
        OrderBook
    }
}
