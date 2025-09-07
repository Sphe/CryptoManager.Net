using CryptoExchange.Net.Objects.Errors;
using CryptoManager.Net.Models;
using CryptoManager.Net.Models.Response;
using CryptoManager.Net.Services.External;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CryptoManager.Net.Controllers;

/// <summary>
/// Controller for DexScreener token pair operations
/// </summary>
[Route("[controller]")]
[AllowAnonymous]
[ResponseCache(Duration = 300, VaryByQueryKeys = ["*"])] // Cache for 5 minutes
public class DexScreenerController : ApiController
{
    private readonly IDexScreenerService _dexscreenerService;
    private readonly ILogger<DexScreenerController> _logger;

    public DexScreenerController(
        IDexScreenerService dexscreenerService,
        ILogger<DexScreenerController> logger) : base(null!)
    {
        _dexscreenerService = dexscreenerService;
        _logger = logger;
    }

    /// <summary>
    /// Gets token pairs for a specific token address
    /// </summary>
    /// <param name="tokenAddress">The token contract address</param>
    /// <param name="minLiquidityUsd">Minimum liquidity in USD (optional)</param>
    /// <param name="minVolume24h">Minimum 24h volume (optional)</param>
    /// <param name="dexIds">Comma-separated list of DEX IDs to filter by (optional)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of token pairs for the specified token</returns>
    /// <response code="200">Returns the list of token pairs</response>
    /// <response code="400">If the token address is invalid</response>
    /// <response code="500">If there was an error fetching data from DexScreener API</response>
    [HttpGet("token-pairs/{tokenAddress}")]
    public async Task<ApiResult<IEnumerable<DexScreenerTokenPair>>> GetTokenPairsAsync(
        [FromRoute] string tokenAddress,
        [FromQuery] decimal? minLiquidityUsd = null,
        [FromQuery] decimal? minVolume24h = null,
        [FromQuery] string? dexIds = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(tokenAddress))
            {
                return ApiResult<IEnumerable<DexScreenerTokenPair>>.Error(ErrorType.Unknown, "400", "Token address is required");
            }

            _logger.LogInformation("Fetching token pairs for {TokenAddress} with filters: minLiquidity={MinLiquidity}, minVolume24h={MinVolume24h}, dexIds={DexIds}",
                tokenAddress, minLiquidityUsd, minVolume24h, dexIds);

            string[]? dexIdArray = null;
            if (!string.IsNullOrWhiteSpace(dexIds))
            {
                dexIdArray = dexIds.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(d => d.Trim())
                    .ToArray();
            }

            var pairs = await _dexscreenerService.GetTokenPairsAsync(
                tokenAddress, 
                minLiquidityUsd, 
                minVolume24h, 
                dexIdArray, 
                cancellationToken);

            _logger.LogInformation("Successfully retrieved {Count} token pairs for {TokenAddress}", pairs.Count(), tokenAddress);

            return ApiResult<IEnumerable<DexScreenerTokenPair>>.Ok(pairs);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Failed to fetch token pairs from DexScreener API for {TokenAddress}", tokenAddress);
            return ApiResult<IEnumerable<DexScreenerTokenPair>>.Error(ErrorType.Unknown, null, ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while fetching token pairs for {TokenAddress}", tokenAddress);
            return ApiResult<IEnumerable<DexScreenerTokenPair>>.Error(ErrorType.Unknown, null, "An unexpected error occurred while fetching token pairs");
        }
    }

    /// <summary>
    /// Gets token pairs for multiple token addresses
    /// </summary>
    /// <param name="tokenAddresses">Comma-separated list of token contract addresses</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of token pairs for the specified tokens</returns>
    /// <response code="200">Returns the list of token pairs</response>
    /// <response code="400">If no token addresses are provided</response>
    /// <response code="500">If there was an error fetching data from DexScreener API</response>
    [HttpGet("token-pairs")]
    public async Task<ApiResult<IEnumerable<DexScreenerTokenPair>>> GetMultipleTokenPairsAsync(
        [FromQuery] string tokenAddresses,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(tokenAddresses))
            {
                return ApiResult<IEnumerable<DexScreenerTokenPair>>.Error(ErrorType.Unknown, "400", "Token addresses are required");
            }

            var addresses = tokenAddresses.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(a => a.Trim())
                .Where(a => !string.IsNullOrWhiteSpace(a))
                .ToArray();

            if (addresses.Length == 0)
            {
                return ApiResult<IEnumerable<DexScreenerTokenPair>>.Error(ErrorType.Unknown, "400", "At least one valid token address is required");
            }

            _logger.LogInformation("Fetching token pairs for {Count} tokens: {TokenAddresses}", addresses.Length, string.Join(", ", addresses));

            var pairs = await _dexscreenerService.GetTokenPairsAsync(addresses, cancellationToken);

            _logger.LogInformation("Successfully retrieved {Count} token pairs for {Count} tokens", pairs.Count(), addresses.Length);

            return ApiResult<IEnumerable<DexScreenerTokenPair>>.Ok(pairs);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Failed to fetch token pairs from DexScreener API for multiple tokens");
            return ApiResult<IEnumerable<DexScreenerTokenPair>>.Error(ErrorType.Unknown, null, ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while fetching token pairs for multiple tokens");
            return ApiResult<IEnumerable<DexScreenerTokenPair>>.Error(ErrorType.Unknown, null, "An unexpected error occurred while fetching token pairs");
        }
    }

    /// <summary>
    /// Gets the top token pairs by liquidity for a specific token
    /// </summary>
    /// <param name="tokenAddress">The token contract address</param>
    /// <param name="limit">Maximum number of pairs to return (default: 10)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of top token pairs by liquidity</returns>
    /// <response code="200">Returns the list of top token pairs by liquidity</response>
    /// <response code="400">If the token address is invalid</response>
    /// <response code="500">If there was an error fetching data from DexScreener API</response>
    [HttpGet("token-pairs/{tokenAddress}/top-by-liquidity")]
    public async Task<ApiResult<IEnumerable<DexScreenerTokenPair>>> GetTopPairsByLiquidityAsync(
        [FromRoute] string tokenAddress,
        [FromQuery] int limit = 10,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(tokenAddress))
            {
                return ApiResult<IEnumerable<DexScreenerTokenPair>>.Error(ErrorType.Unknown, "400", "Token address is required");
            }

            if (limit <= 0 || limit > 100)
            {
                return ApiResult<IEnumerable<DexScreenerTokenPair>>.Error(ErrorType.Unknown, "400", "Limit must be between 1 and 100");
            }

            _logger.LogInformation("Fetching top {Limit} token pairs by liquidity for {TokenAddress}", limit, tokenAddress);

            var pairs = await _dexscreenerService.GetTokenPairsAsync(tokenAddress, cancellationToken: cancellationToken);
            var topPairs = pairs
                .Where(p => p.Liquidity?.Usd.HasValue == true)
                .OrderByDescending(p => p.Liquidity!.Usd)
                .Take(limit);

            _logger.LogInformation("Successfully retrieved top {Count} token pairs by liquidity for {TokenAddress}", topPairs.Count(), tokenAddress);

            return ApiResult<IEnumerable<DexScreenerTokenPair>>.Ok(topPairs);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Failed to fetch top token pairs from DexScreener API for {TokenAddress}", tokenAddress);
            return ApiResult<IEnumerable<DexScreenerTokenPair>>.Error(ErrorType.Unknown, null, ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while fetching top token pairs for {TokenAddress}", tokenAddress);
            return ApiResult<IEnumerable<DexScreenerTokenPair>>.Error(ErrorType.Unknown, null, "An unexpected error occurred while fetching top token pairs");
        }
    }

    /// <summary>
    /// Gets the top token pairs by volume for a specific token
    /// </summary>
    /// <param name="tokenAddress">The token contract address</param>
    /// <param name="limit">Maximum number of pairs to return (default: 10)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of top token pairs by 24h volume</returns>
    /// <response code="200">Returns the list of top token pairs by volume</response>
    /// <response code="400">If the token address is invalid</response>
    /// <response code="500">If there was an error fetching data from DexScreener API</response>
    [HttpGet("token-pairs/{tokenAddress}/top-by-volume")]
    public async Task<ApiResult<IEnumerable<DexScreenerTokenPair>>> GetTopPairsByVolumeAsync(
        [FromRoute] string tokenAddress,
        [FromQuery] int limit = 10,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(tokenAddress))
            {
                return ApiResult<IEnumerable<DexScreenerTokenPair>>.Error(ErrorType.Unknown, "400", "Token address is required");
            }

            if (limit <= 0 || limit > 100)
            {
                return ApiResult<IEnumerable<DexScreenerTokenPair>>.Error(ErrorType.Unknown, "400", "Limit must be between 1 and 100");
            }

            _logger.LogInformation("Fetching top {Limit} token pairs by volume for {TokenAddress}", limit, tokenAddress);

            var pairs = await _dexscreenerService.GetTokenPairsAsync(tokenAddress, cancellationToken: cancellationToken);
            var topPairs = pairs
                .Where(p => p.Volume?.H24.HasValue == true)
                .OrderByDescending(p => p.Volume!.H24)
                .Take(limit);

            _logger.LogInformation("Successfully retrieved top {Count} token pairs by volume for {TokenAddress}", topPairs.Count(), tokenAddress);

            return ApiResult<IEnumerable<DexScreenerTokenPair>>.Ok(topPairs);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Failed to fetch top token pairs from DexScreener API for {TokenAddress}", tokenAddress);
            return ApiResult<IEnumerable<DexScreenerTokenPair>>.Error(ErrorType.Unknown, null, ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while fetching top token pairs for {TokenAddress}", tokenAddress);
            return ApiResult<IEnumerable<DexScreenerTokenPair>>.Error(ErrorType.Unknown, null, "An unexpected error occurred while fetching top token pairs");
        }
    }
}
