using System.Text.Json.Serialization;

namespace CryptoManager.Net.Models;

/// <summary>
/// Represents a token pair from DexScreener API
/// </summary>
public class DexScreenerTokenPair
{
    /// <summary>
    /// The blockchain chain ID (e.g., "solana")
    /// </summary>
    [JsonPropertyName("chainId")]
    public string ChainId { get; set; } = string.Empty;

    /// <summary>
    /// The DEX identifier (e.g., "orca", "raydium")
    /// </summary>
    [JsonPropertyName("dexId")]
    public string DexId { get; set; } = string.Empty;

    /// <summary>
    /// URL to the pair on DexScreener
    /// </summary>
    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// The pair contract address
    /// </summary>
    [JsonPropertyName("pairAddress")]
    public string PairAddress { get; set; } = string.Empty;

    /// <summary>
    /// Labels for the pair (e.g., "wp", "CLMM", "DLMM")
    /// </summary>
    [JsonPropertyName("labels")]
    public string[] Labels { get; set; } = Array.Empty<string>();

    /// <summary>
    /// The base token information
    /// </summary>
    [JsonPropertyName("baseToken")]
    public DexScreenerToken BaseToken { get; set; } = new();

    /// <summary>
    /// The quote token information
    /// </summary>
    [JsonPropertyName("quoteToken")]
    public DexScreenerToken QuoteToken { get; set; } = new();

    /// <summary>
    /// Price in native token units
    /// </summary>
    [JsonPropertyName("priceNative")]
    public string PriceNative { get; set; } = string.Empty;

    /// <summary>
    /// Price in USD
    /// </summary>
    [JsonPropertyName("priceUsd")]
    public string PriceUsd { get; set; } = string.Empty;

    /// <summary>
    /// Transaction statistics
    /// </summary>
    [JsonPropertyName("txns")]
    public DexScreenerTransactions? Transactions { get; set; }

    /// <summary>
    /// Volume statistics
    /// </summary>
    [JsonPropertyName("volume")]
    public DexScreenerVolume? Volume { get; set; }

    /// <summary>
    /// Price change statistics
    /// </summary>
    [JsonPropertyName("priceChange")]
    public DexScreenerPriceChange? PriceChange { get; set; }

    /// <summary>
    /// Liquidity information
    /// </summary>
    [JsonPropertyName("liquidity")]
    public DexScreenerLiquidity? Liquidity { get; set; }

    /// <summary>
    /// Fully diluted valuation
    /// </summary>
    [JsonPropertyName("fdv")]
    public decimal? Fdv { get; set; }

    /// <summary>
    /// Market capitalization
    /// </summary>
    [JsonPropertyName("marketCap")]
    public decimal? MarketCap { get; set; }

    /// <summary>
    /// Pair creation timestamp
    /// </summary>
    [JsonPropertyName("pairCreatedAt")]
    public long PairCreatedAt { get; set; }

    /// <summary>
    /// Additional information about the token
    /// </summary>
    [JsonPropertyName("info")]
    public DexScreenerTokenInfo? Info { get; set; }
}

/// <summary>
/// Represents a token in a DexScreener pair
/// </summary>
public class DexScreenerToken
{
    /// <summary>
    /// Token contract address
    /// </summary>
    [JsonPropertyName("address")]
    public string Address { get; set; } = string.Empty;

    /// <summary>
    /// Token name
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Token symbol
    /// </summary>
    [JsonPropertyName("symbol")]
    public string Symbol { get; set; } = string.Empty;
}

/// <summary>
/// Transaction statistics for different time periods
/// </summary>
public class DexScreenerTransactions
{
    /// <summary>
    /// 5-minute transaction stats
    /// </summary>
    [JsonPropertyName("m5")]
    public DexScreenerTransactionStats? M5 { get; set; }

    /// <summary>
    /// 1-hour transaction stats
    /// </summary>
    [JsonPropertyName("h1")]
    public DexScreenerTransactionStats? H1 { get; set; }

    /// <summary>
    /// 6-hour transaction stats
    /// </summary>
    [JsonPropertyName("h6")]
    public DexScreenerTransactionStats? H6 { get; set; }

    /// <summary>
    /// 24-hour transaction stats
    /// </summary>
    [JsonPropertyName("h24")]
    public DexScreenerTransactionStats? H24 { get; set; }
}

/// <summary>
/// Transaction statistics for a specific time period
/// </summary>
public class DexScreenerTransactionStats
{
    /// <summary>
    /// Number of buy transactions
    /// </summary>
    [JsonPropertyName("buys")]
    public int Buys { get; set; }

    /// <summary>
    /// Number of sell transactions
    /// </summary>
    [JsonPropertyName("sells")]
    public int Sells { get; set; }
}

/// <summary>
/// Volume statistics for different time periods
/// </summary>
public class DexScreenerVolume
{
    /// <summary>
    /// 5-minute volume
    /// </summary>
    [JsonPropertyName("m5")]
    public decimal? M5 { get; set; }

    /// <summary>
    /// 1-hour volume
    /// </summary>
    [JsonPropertyName("h1")]
    public decimal? H1 { get; set; }

    /// <summary>
    /// 6-hour volume
    /// </summary>
    [JsonPropertyName("h6")]
    public decimal? H6 { get; set; }

    /// <summary>
    /// 24-hour volume
    /// </summary>
    [JsonPropertyName("h24")]
    public decimal? H24 { get; set; }
}

/// <summary>
/// Price change statistics for different time periods
/// </summary>
public class DexScreenerPriceChange
{
    /// <summary>
    /// 5-minute price change percentage
    /// </summary>
    [JsonPropertyName("m5")]
    public decimal? M5 { get; set; }

    /// <summary>
    /// 1-hour price change percentage
    /// </summary>
    [JsonPropertyName("h1")]
    public decimal? H1 { get; set; }

    /// <summary>
    /// 6-hour price change percentage
    /// </summary>
    [JsonPropertyName("h6")]
    public decimal? H6 { get; set; }

    /// <summary>
    /// 24-hour price change percentage
    /// </summary>
    [JsonPropertyName("h24")]
    public decimal? H24 { get; set; }
}

/// <summary>
/// Liquidity information
/// </summary>
public class DexScreenerLiquidity
{
    /// <summary>
    /// Liquidity in USD
    /// </summary>
    [JsonPropertyName("usd")]
    public decimal? Usd { get; set; }

    /// <summary>
    /// Base token liquidity
    /// </summary>
    [JsonPropertyName("base")]
    public decimal? Base { get; set; }

    /// <summary>
    /// Quote token liquidity
    /// </summary>
    [JsonPropertyName("quote")]
    public decimal? Quote { get; set; }
}

/// <summary>
/// Additional token information
/// </summary>
public class DexScreenerTokenInfo
{
    /// <summary>
    /// Token image URL
    /// </summary>
    [JsonPropertyName("imageUrl")]
    public string? ImageUrl { get; set; }

    /// <summary>
    /// Header image URL
    /// </summary>
    [JsonPropertyName("header")]
    public string? Header { get; set; }

    /// <summary>
    /// OpenGraph image URL
    /// </summary>
    [JsonPropertyName("openGraph")]
    public string? OpenGraph { get; set; }

    /// <summary>
    /// Website URLs
    /// </summary>
    [JsonPropertyName("websites")]
    public DexScreenerWebsite[]? Websites { get; set; }

    /// <summary>
    /// Social media links
    /// </summary>
    [JsonPropertyName("socials")]
    public DexScreenerSocial[]? Socials { get; set; }
}

/// <summary>
/// Website information
/// </summary>
public class DexScreenerWebsite
{
    /// <summary>
    /// Website label
    /// </summary>
    [JsonPropertyName("label")]
    public string Label { get; set; } = string.Empty;

    /// <summary>
    /// Website URL
    /// </summary>
    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;
}

/// <summary>
/// Social media information
/// </summary>
public class DexScreenerSocial
{
    /// <summary>
    /// Social media type (e.g., "twitter", "discord")
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Social media URL
    /// </summary>
    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;
}
