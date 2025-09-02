using System.Text.Json.Serialization;

namespace CryptoManager.Net.UI.Models.ApiModels.Requests
{
    public class LoginRequest
    {
        [JsonPropertyName("email")]
        public string Email { get; set; } = string.Empty;
        [JsonPropertyName("password")]
        public string Password { get; set; } = string.Empty;
    }
}
