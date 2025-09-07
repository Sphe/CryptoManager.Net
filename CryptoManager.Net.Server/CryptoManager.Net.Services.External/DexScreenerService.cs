using CryptoManager.Net.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace CryptoManager.Net.Services.External;

/// <summary>
/// Service for fetching token pair data from DexScreener API
/// </summary>
public interface IDexScreenerService
{
    /// <summary>
    /// Gets token pairs for a specific token address
    /// </summary>
    /// <param name="tokenAddress">The token contract address</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of token pairs</returns>
    Task<IEnumerable<DexScreenerTokenPair>> GetTokenPairsAsync(string tokenAddress, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets token pairs for multiple token addresses
    /// </summary>
    /// <param name="tokenAddresses">Array of token contract addresses</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of token pairs</returns>
    Task<IEnumerable<DexScreenerTokenPair>> GetTokenPairsAsync(string[] tokenAddresses, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets token pairs with optional filtering
    /// </summary>
    /// <param name="tokenAddress">The token contract address</param>
    /// <param name="minLiquidityUsd">Minimum liquidity in USD</param>
    /// <param name="minVolume24h">Minimum 24h volume</param>
    /// <param name="dexIds">Filter by specific DEX IDs</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Filtered list of token pairs</returns>
    Task<IEnumerable<DexScreenerTokenPair>> GetTokenPairsAsync(
        string tokenAddress,
        decimal? minLiquidityUsd = null,
        decimal? minVolume24h = null,
        string[]? dexIds = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Implementation of DexScreener service
/// </summary>
public class DexScreenerService : IDexScreenerService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<DexScreenerService> _logger;
    private readonly IMemoryCache _cache;
    private readonly JsonSerializerOptions _jsonOptions;

    private const string DEXSCREENER_API_BASE_URL = "https://api.dexscreener.com";
    private const string TOKEN_PAIRS_ENDPOINT = "/tokens/v1/solana";
    private static readonly TimeSpan CacheExpiration = TimeSpan.FromMinutes(5); // Cache for 5 minutes

    public DexScreenerService(
        HttpClient httpClient,
        ILogger<DexScreenerService> logger,
        IMemoryCache cache)
    {
        _httpClient = httpClient;
        _logger = logger;
        _cache = cache;

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
        };

        // Configure HttpClient
        _httpClient.BaseAddress = new Uri(DEXSCREENER_API_BASE_URL);
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "CryptoManager.Net/1.0");
    }

    /// <inheritdoc />
    public async Task<IEnumerable<DexScreenerTokenPair>> GetTokenPairsAsync(string tokenAddress, CancellationToken cancellationToken = default)
    {
        return await GetTokenPairsAsync(tokenAddress, null, null, null, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<DexScreenerTokenPair>> GetTokenPairsAsync(string[] tokenAddresses, CancellationToken cancellationToken = default)
    {
        try
        {
            if (tokenAddresses == null || tokenAddresses.Length == 0)
            {
                return Enumerable.Empty<DexScreenerTokenPair>();
            }

            _logger.LogInformation("Fetching token pairs for {Count} tokens from DexScreener API", tokenAddresses.Length);

            // Create cache key for multiple addresses
            var cacheKey = $"dexscreener_pairs_{string.Join("_", tokenAddresses.OrderBy(x => x))}";

            // Try to get from cache first
            if (_cache.TryGetValue(cacheKey, out DexScreenerTokenPair[]? cachedPairs) && cachedPairs != null)
            {
                _logger.LogDebug("Returning {Count} token pairs from cache", cachedPairs.Length);
                return cachedPairs;
            }

            // For multiple addresses, we need to make separate calls or use a different endpoint
            // For now, let's make separate calls and combine results
            var allPairs = new List<DexScreenerTokenPair>();
            
            foreach (var address in tokenAddresses)
            {
                var endpoint = $"{TOKEN_PAIRS_ENDPOINT}/{address}";
                var response = await _httpClient.GetAsync(endpoint, cancellationToken);
                response.EnsureSuccessStatusCode();

                var jsonContent = await response.Content.ReadAsStringAsync(cancellationToken);
                var pairs = JsonSerializer.Deserialize<DexScreenerTokenPair[]>(jsonContent, _jsonOptions);

                if (pairs != null)
                {
                    allPairs.AddRange(pairs);
                }
            }

            // Cache the results
            _cache.Set(cacheKey, allPairs.ToArray(), CacheExpiration);
            _logger.LogInformation("Successfully fetched and cached {Count} token pairs from DexScreener API", allPairs.Count);

            return allPairs;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error while fetching token pairs from DexScreener API");
            throw new InvalidOperationException("Failed to fetch token pairs from DexScreener API", ex);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "JSON deserialization error while parsing DexScreener API response");
            throw new InvalidOperationException("Failed to parse DexScreener API response", ex);
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            _logger.LogError(ex, "Timeout while fetching token pairs from DexScreener API");
            throw new InvalidOperationException("Timeout while fetching token pairs from DexScreener API", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while fetching token pairs from DexScreener API");
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<IEnumerable<DexScreenerTokenPair>> GetTokenPairsAsync(
        string tokenAddress,
        decimal? minLiquidityUsd = null,
        decimal? minVolume24h = null,
        string[]? dexIds = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(tokenAddress))
            {
                return Enumerable.Empty<DexScreenerTokenPair>();
            }

            _logger.LogInformation("Fetching token pairs for {TokenAddress} from DexScreener API with filters: minLiquidity={MinLiquidity}, minVolume24h={MinVolume24h}, dexIds={DexIds}",
                tokenAddress, minLiquidityUsd, minVolume24h, dexIds != null ? string.Join(",", dexIds) : "none");

            // Create cache key
            var cacheKey = $"dexscreener_pairs_{tokenAddress}";

            // Try to get from cache first
            if (_cache.TryGetValue(cacheKey, out DexScreenerTokenPair[]? cachedPairs) && cachedPairs != null)
            {
                _logger.LogDebug("Returning {Count} token pairs from cache", cachedPairs.Length);
                return ApplyFilters(cachedPairs, minLiquidityUsd, minVolume24h, dexIds);
            }

            // Fetch from API
            var endpoint = $"{TOKEN_PAIRS_ENDPOINT}/{tokenAddress}";
            var response = await _httpClient.GetAsync(endpoint, cancellationToken);
            response.EnsureSuccessStatusCode();

            var jsonContent = await response.Content.ReadAsStringAsync(cancellationToken);
            var pairs = JsonSerializer.Deserialize<DexScreenerTokenPair[]>(jsonContent, _jsonOptions);

            if (pairs == null)
            {
                _logger.LogWarning("Failed to deserialize DexScreener token pairs response");
                return Enumerable.Empty<DexScreenerTokenPair>();
            }

            // Cache the results
            _cache.Set(cacheKey, pairs, CacheExpiration);
            _logger.LogInformation("Successfully fetched and cached {Count} token pairs from DexScreener API", pairs.Length);

            return ApplyFilters(pairs, minLiquidityUsd, minVolume24h, dexIds);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error while fetching token pairs from DexScreener API");
            throw new InvalidOperationException("Failed to fetch token pairs from DexScreener API", ex);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "JSON deserialization error while parsing DexScreener API response");
            throw new InvalidOperationException("Failed to parse DexScreener API response", ex);
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            _logger.LogError(ex, "Timeout while fetching token pairs from DexScreener API");
            throw new InvalidOperationException("Timeout while fetching token pairs from DexScreener API", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while fetching token pairs from DexScreener API");
            throw;
        }
    }

    /// <summary>
    /// Applies filters to the token pairs list
    /// </summary>
    private static IEnumerable<DexScreenerTokenPair> ApplyFilters(
        DexScreenerTokenPair[] pairs,
        decimal? minLiquidityUsd,
        decimal? minVolume24h,
        string[]? dexIds)
    {
        var filteredPairs = pairs.AsEnumerable();

        if (minLiquidityUsd.HasValue)
        {
            filteredPairs = filteredPairs.Where(p => p.Liquidity?.Usd >= minLiquidityUsd.Value);
        }

        if (minVolume24h.HasValue)
        {
            filteredPairs = filteredPairs.Where(p => p.Volume?.H24 >= minVolume24h.Value);
        }

        if (dexIds != null && dexIds.Length > 0)
        {
            filteredPairs = filteredPairs.Where(p => dexIds.Contains(p.DexId, StringComparer.OrdinalIgnoreCase));
        }

        return filteredPairs;
    }
}
