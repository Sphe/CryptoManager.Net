using CryptoManager.Net.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Text.Json;

namespace CryptoManager.Net.Services.External;

/// <summary>
/// Service for fetching verified tokens from Jupiter API
/// </summary>
public interface IJupiterTokenService
{
    /// <summary>
    /// Gets the list of verified tokens from Jupiter API
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of verified Jupiter tokens</returns>
    Task<IEnumerable<JupiterToken>> GetVerifiedTokensAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets verified tokens with optional filtering
    /// </summary>
    /// <param name="minDailyVolume">Minimum daily volume filter</param>
    /// <param name="tags">Tags to filter by</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Filtered list of verified Jupiter tokens</returns>
    Task<IEnumerable<JupiterToken>> GetVerifiedTokensAsync(
        decimal? minDailyVolume = null, 
        string[]? tags = null, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets current prices for multiple tokens
    /// </summary>
    /// <param name="tokenAddresses">Array of token addresses to get prices for</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Dictionary of token addresses and their price data</returns>
    Task<JupiterPriceResponse> GetTokenPricesAsync(
        string[] tokenAddresses, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets current price for a single token
    /// </summary>
    /// <param name="tokenAddress">Token address to get price for</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Price data for the token, or null if not found</returns>
    Task<JupiterTokenPrice?> GetTokenPriceAsync(
        string tokenAddress, 
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Implementation of Jupiter token service
/// </summary>
public class JupiterTokenService : IJupiterTokenService, IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<JupiterTokenService> _logger;
    private readonly IMemoryCache _cache;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly SemaphoreSlim _rateLimitSemaphore;
    private readonly ConcurrentQueue<DateTime> _apiCallTimes;

    private const string JUPITER_API_BASE_URL = "https://lite-api.jup.ag";
    private const string VERIFIED_TOKENS_ENDPOINT = "/tokens/v1/tagged/verified";
    private const string PRICE_ENDPOINT = "/price/v3";
    private const string CACHE_KEY = "jupiter_verified_tokens";
    private const string PRICE_CACHE_KEY_PREFIX = "jupiter_price_";
    private static readonly TimeSpan CacheExpiration = TimeSpan.FromHours(24); // Cache for 24 hours
    private static readonly TimeSpan PriceCacheExpiration = TimeSpan.FromMinutes(5); // Cache prices for 5 minutes
    private const int MAX_CALLS_PER_MINUTE = 30;
    private static readonly TimeSpan RateLimitWindow = TimeSpan.FromMinutes(1);

    public JupiterTokenService(
        HttpClient httpClient,
        ILogger<JupiterTokenService> logger,
        IMemoryCache cache)
    {
        _httpClient = httpClient;
        _logger = logger;
        _cache = cache;
        _rateLimitSemaphore = new SemaphoreSlim(MAX_CALLS_PER_MINUTE, MAX_CALLS_PER_MINUTE);
        _apiCallTimes = new ConcurrentQueue<DateTime>();

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
        };

        // Configure HttpClient
        _httpClient.BaseAddress = new Uri(JUPITER_API_BASE_URL);
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "CryptoManager.Net/1.0");
    }

    /// <inheritdoc />
    public async Task<IEnumerable<JupiterToken>> GetVerifiedTokensAsync(CancellationToken cancellationToken = default)
    {
        return await GetVerifiedTokensAsync(null, null, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<JupiterToken>> GetVerifiedTokensAsync(
        decimal? minDailyVolume = null, 
        string[]? tags = null, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Try to get from cache first
            if (_cache.TryGetValue(CACHE_KEY, out JupiterToken[]? cachedTokens) && cachedTokens != null)
            {
                _logger.LogDebug("Returning {Count} verified tokens from cache", cachedTokens.Length);
                return ApplyFilters(cachedTokens, minDailyVolume, tags);
            }

            // Fetch from API
            _logger.LogInformation("Fetching verified tokens from Jupiter API");
            var response = await _httpClient.GetAsync(VERIFIED_TOKENS_ENDPOINT, cancellationToken);
            response.EnsureSuccessStatusCode();

            var jsonContent = await response.Content.ReadAsStringAsync(cancellationToken);
            var tokens = JsonSerializer.Deserialize<JupiterToken[]>(jsonContent, _jsonOptions);

            if (tokens == null)
            {
                _logger.LogWarning("Failed to deserialize Jupiter tokens response");
                return Enumerable.Empty<JupiterToken>();
            }

            // Cache the results
            _cache.Set(CACHE_KEY, tokens, CacheExpiration);
            _logger.LogInformation("Successfully fetched and cached {Count} verified tokens from Jupiter API", tokens.Length);

            return ApplyFilters(tokens, minDailyVolume, tags);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error while fetching verified tokens from Jupiter API");
            throw new InvalidOperationException("Failed to fetch verified tokens from Jupiter API", ex);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "JSON deserialization error while parsing Jupiter API response");
            throw new InvalidOperationException("Failed to parse Jupiter API response", ex);
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            _logger.LogError(ex, "Timeout while fetching verified tokens from Jupiter API");
            throw new InvalidOperationException("Timeout while fetching verified tokens from Jupiter API", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while fetching verified tokens from Jupiter API");
            throw;
        }
    }

    /// <summary>
    /// Applies filters to the token list
    /// </summary>
    private static IEnumerable<JupiterToken> ApplyFilters(
        JupiterToken[] tokens, 
        decimal? minDailyVolume, 
        string[]? tags)
    {
        var filteredTokens = tokens.AsEnumerable();

        if (minDailyVolume.HasValue)
        {
            filteredTokens = filteredTokens.Where(t => t.DailyVolume >= minDailyVolume.Value);
        }

        if (tags != null && tags.Length > 0)
        {
            filteredTokens = filteredTokens.Where(t => tags.Any(tag => t.Tags.Contains(tag, StringComparer.OrdinalIgnoreCase)));
        }

        return filteredTokens;
    }

    /// <summary>
    /// Gets current prices for multiple tokens
    /// </summary>
    public async Task<JupiterPriceResponse> GetTokenPricesAsync(
        string[] tokenAddresses, 
        CancellationToken cancellationToken = default)
    {
        if (tokenAddresses == null || tokenAddresses.Length == 0)
        {
            return new JupiterPriceResponse();
        }

        // Create cache key from sorted token addresses
        var sortedAddresses = tokenAddresses.OrderBy(x => x).ToArray();
        var cacheKey = $"{PRICE_CACHE_KEY_PREFIX}{string.Join(",", sortedAddresses)}";

        if (_cache.TryGetValue(cacheKey, out JupiterPriceResponse? cachedPrices) && cachedPrices != null)
        {
            _logger.LogDebug("Returning cached Jupiter prices for {TokenCount} tokens", tokenAddresses.Length);
            return cachedPrices;
        }

        // Apply rate limiting before making API call
        await EnforceRateLimitAsync(cancellationToken);

        try
        {
            var idsParam = string.Join(",", tokenAddresses);
            var url = $"{JUPITER_API_BASE_URL}{PRICE_ENDPOINT}?ids={idsParam}";

            _logger.LogDebug("Fetching Jupiter prices for {TokenCount} tokens from {Url}", tokenAddresses.Length, url);

            var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            var jsonContent = await response.Content.ReadAsStringAsync(cancellationToken);
            var prices = JsonSerializer.Deserialize<JupiterPriceResponse>(jsonContent, _jsonOptions);

            if (prices == null)
            {
                _logger.LogWarning("Jupiter API returned null prices for tokens: {TokenAddresses}", string.Join(", ", tokenAddresses));
                return new JupiterPriceResponse();
            }

            // Cache the results
            _cache.Set(cacheKey, prices, PriceCacheExpiration);

            _logger.LogDebug("Successfully fetched Jupiter prices for {TokenCount} tokens", prices.Count);
            return prices;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error while fetching Jupiter prices for tokens: {TokenAddresses}", string.Join(", ", tokenAddresses));
            throw new InvalidOperationException("Failed to fetch Jupiter prices from API", ex);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "JSON deserialization error while parsing Jupiter price response for tokens: {TokenAddresses}", string.Join(", ", tokenAddresses));
            throw new InvalidOperationException("Failed to parse Jupiter price API response", ex);
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            _logger.LogError(ex, "Timeout while fetching Jupiter prices for tokens: {TokenAddresses}", string.Join(", ", tokenAddresses));
            throw new InvalidOperationException("Timeout while fetching Jupiter prices from API", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while fetching Jupiter prices for tokens: {TokenAddresses}", string.Join(", ", tokenAddresses));
            throw;
        }
    }

    /// <summary>
    /// Gets current price for a single token
    /// </summary>
    public async Task<JupiterTokenPrice?> GetTokenPriceAsync(
        string tokenAddress, 
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(tokenAddress))
        {
            return null;
        }

        // Apply rate limiting
        await EnforceRateLimitAsync(cancellationToken);

        var prices = await GetTokenPricesAsync(new[] { tokenAddress }, cancellationToken);
        return prices.TryGetValue(tokenAddress, out var price) ? price : null;
    }

    /// <summary>
    /// Enforces rate limiting to ensure we don't exceed 30 calls per minute
    /// </summary>
    private async Task EnforceRateLimitAsync(CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var cutoffTime = now - RateLimitWindow;

        // Remove old API call times that are outside the rate limit window
        while (_apiCallTimes.TryPeek(out var oldestCall) && oldestCall < cutoffTime)
        {
            _apiCallTimes.TryDequeue(out _);
        }

        // Check if we've reached the rate limit
        if (_apiCallTimes.Count >= MAX_CALLS_PER_MINUTE)
        {
            var oldestRemainingCall = _apiCallTimes.TryPeek(out var oldest) ? oldest : now;
            var waitTime = oldestRemainingCall.Add(RateLimitWindow) - now;
            
            if (waitTime > TimeSpan.Zero)
            {
                _logger.LogWarning("Rate limit reached. Waiting {WaitTime}ms before making API call", waitTime.TotalMilliseconds);
                await Task.Delay(waitTime, cancellationToken);
                
                // Clean up old entries after waiting
                now = DateTime.UtcNow;
                cutoffTime = now - RateLimitWindow;
                while (_apiCallTimes.TryPeek(out var callTime) && callTime < cutoffTime)
                {
                    _apiCallTimes.TryDequeue(out _);
                }
            }
        }

        // Record this API call
        _apiCallTimes.Enqueue(now);
        _logger.LogDebug("API call recorded. Current calls in window: {CallCount}", _apiCallTimes.Count);
    }

    /// <summary>
    /// Disposes the service and cleans up resources
    /// </summary>
    public void Dispose()
    {
        _rateLimitSemaphore?.Dispose();
    }
}
