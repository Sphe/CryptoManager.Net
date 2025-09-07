using System.Text.Json.Serialization;

namespace CryptoManager.Net.Models;

/// <summary>
/// Represents a verified token from Jupiter API
/// </summary>
public class JupiterToken
{
    /// <summary>
    /// The token's mint address
    /// </summary>
    [JsonPropertyName("address")]
    public string Address { get; set; } = string.Empty;

    /// <summary>
    /// The token's name
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The token's symbol
    /// </summary>
    [JsonPropertyName("symbol")]
    public string Symbol { get; set; } = string.Empty;

    /// <summary>
    /// The token's decimal places
    /// </summary>
    [JsonPropertyName("decimals")]
    public int Decimals { get; set; }

    /// <summary>
    /// The token's logo URI
    /// </summary>
    [JsonPropertyName("logoURI")]
    public string? LogoUri { get; set; }

    /// <summary>
    /// The token's tags (e.g., verified, community, etc.)
    /// </summary>
    [JsonPropertyName("tags")]
    public string[] Tags { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Daily trading volume
    /// </summary>
    [JsonPropertyName("daily_volume")]
    public decimal DailyVolume { get; set; }

    /// <summary>
    /// Creation timestamp
    /// </summary>
    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Freeze authority address
    /// </summary>
    [JsonPropertyName("freeze_authority")]
    public string? FreezeAuthority { get; set; }

    /// <summary>
    /// Mint authority address
    /// </summary>
    [JsonPropertyName("mint_authority")]
    public string? MintAuthority { get; set; }

    /// <summary>
    /// Permanent delegate address
    /// </summary>
    [JsonPropertyName("permanent_delegate")]
    public string? PermanentDelegate { get; set; }

    /// <summary>
    /// Mint timestamp
    /// </summary>
    [JsonPropertyName("minted_at")]
    public DateTime? MintedAt { get; set; }

    /// <summary>
    /// Token extensions (e.g., CoinGecko ID)
    /// </summary>
    [JsonPropertyName("extensions")]
    public JupiterTokenExtensions? Extensions { get; set; }
}

/// <summary>
/// Token extensions containing additional metadata
/// </summary>
public class JupiterTokenExtensions
{
    /// <summary>
    /// CoinGecko ID for the token
    /// </summary>
    [JsonPropertyName("coingeckoId")]
    public string? CoinGeckoId { get; set; }
}

/// <summary>
/// Jupiter price data for a specific token
/// </summary>
public class JupiterTokenPrice
{
    /// <summary>
    /// USD price of the token
    /// </summary>
    [JsonPropertyName("usdPrice")]
    public decimal UsdPrice { get; set; }

    /// <summary>
    /// Block ID when the price was fetched
    /// </summary>
    [JsonPropertyName("blockId")]
    public long BlockId { get; set; }

    /// <summary>
    /// Number of decimals for the token
    /// </summary>
    [JsonPropertyName("decimals")]
    public int Decimals { get; set; }

    /// <summary>
    /// 24-hour price change percentage
    /// </summary>
    [JsonPropertyName("priceChange24h")]
    public decimal PriceChange24h { get; set; }
}

/// <summary>
/// Response from Jupiter price API containing multiple token prices
/// </summary>
public class JupiterPriceResponse : Dictionary<string, JupiterTokenPrice>
{
}
