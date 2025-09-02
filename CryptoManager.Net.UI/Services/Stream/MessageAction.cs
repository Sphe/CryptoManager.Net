using CryptoManager.Net.UI.Models;
using System.Net.WebSockets;
using System.Text.Json;
using System.Text;
using CryptoManager.Net.UI.Models.ApiModels.Response;
using CryptoManager.Net.UI.Services.Stream;

namespace CryptoManager.Net.UI.Services
{
    public enum MessageAction
    {
        Subscribe,
        Unsubscribe,
        Authenticate
    }
}
