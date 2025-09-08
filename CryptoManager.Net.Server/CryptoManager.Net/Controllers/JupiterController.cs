using CryptoExchange.Net.Objects.Errors;
using CryptoManager.Net.Models;
using CryptoManager.Net.Models.Response;
using CryptoManager.Net.Services.External;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CryptoManager.Net.Controllers;

/// <summary>
/// Controller for Jupiter token operations
/// </summary>
[Route("[controller]")]
[AllowAnonymous]
[ResponseCache(Duration = 900, VaryByQueryKeys = ["*"])] // Cache for 15 minutes
public class JupiterController : ApiController
{
    private readonly IJupiterTokenService _jupiterTokenService;
    private readonly ILogger<JupiterController> _logger;

    public JupiterController(
        IJupiterTokenService jupiterTokenService,
        ILogger<JupiterController> logger) : base(null!)
    {
        _jupiterTokenService = jupiterTokenService;
        _logger = logger;
    }

    /// <summary>
    /// Gets the list of verified tokens from Jupiter
    /// </summary>
    /// <param name="minDailyVolume">Minimum daily volume filter (optional)</param>
    /// <param name="tags">Comma-separated list of tags to filter by (optional)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of verified Jupiter tokens</returns>
    /// <response code="200">Returns the list of verified tokens</response>
    /// <response code="500">If there was an error fetching tokens from Jupiter API</response>
    [HttpGet("verified-tokens")]
    public async Task<ApiResult<IEnumerable<JupiterToken>>> GetVerifiedTokensAsync(
        [FromQuery] decimal? minDailyVolume = null,
        [FromQuery] string? tags = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Fetching verified tokens with filters: minDailyVolume={MinDailyVolume}, tags={Tags}", 
                minDailyVolume, tags);

            string[]? tagArray = null;
            if (!string.IsNullOrWhiteSpace(tags))
            {
                tagArray = tags.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(t => t.Trim())
                    .ToArray();
            }

            var tokens = await _jupiterTokenService.GetVerifiedTokensAsync(minDailyVolume, tagArray, cancellationToken);
            
            _logger.LogInformation("Successfully retrieved {Count} verified tokens", tokens.Count());
            
            return ApiResult<IEnumerable<JupiterToken>>.Ok(tokens);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Failed to fetch verified tokens from Jupiter API");
            return ApiResult<IEnumerable<JupiterToken>>.Error(ErrorType.Unknown, null, ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while fetching verified tokens");
            return ApiResult<IEnumerable<JupiterToken>>.Error(ErrorType.Unknown, null, "An unexpected error occurred while fetching verified tokens");
        }
    }

    /// <summary>
    /// Gets verified tokens with high daily volume (top tokens by volume)
    /// </summary>
    /// <param name="limit">Maximum number of tokens to return (default: 50)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of top verified tokens by daily volume</returns>
    /// <response code="200">Returns the list of top tokens by volume</response>
    /// <response code="500">If there was an error fetching tokens from Jupiter API</response>
    [HttpGet("top-tokens")]
    public async Task<ApiResult<IEnumerable<JupiterToken>>> GetTopTokensAsync(
        [FromQuery] int limit = 50,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Fetching top {Limit} verified tokens by daily volume", limit);

            var tokens = await _jupiterTokenService.GetVerifiedTokensAsync(cancellationToken: cancellationToken);
            var topTokens = tokens
                .OrderByDescending(t => t.DailyVolume)
                .Take(limit);

            _logger.LogInformation("Successfully retrieved top {Count} verified tokens", topTokens.Count());

            return ApiResult<IEnumerable<JupiterToken>>.Ok(topTokens);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Failed to fetch top tokens from Jupiter API");
            return ApiResult<IEnumerable<JupiterToken>>.Error(ErrorType.Unknown, null, ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while fetching top tokens");
            return ApiResult<IEnumerable<JupiterToken>>.Error(ErrorType.Unknown, null, "An unexpected error occurred while fetching top tokens");
        }
    }

    /// <summary>
    /// Gets a specific verified token by address
    /// </summary>
    /// <param name="address">The token's mint address</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The verified token if found</returns>
    /// <response code="200">Returns the token if found</response>
    /// <response code="404">If the token is not found in verified tokens</response>
    /// <response code="500">If there was an error fetching tokens from Jupiter API</response>
    [HttpGet("verified-tokens/{address}")]
    public async Task<ApiResult<JupiterToken?>> GetVerifiedTokenAsync(
        [FromRoute] string address,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Fetching verified token with address: {Address}", address);

            var tokens = await _jupiterTokenService.GetVerifiedTokensAsync(cancellationToken: cancellationToken);
            var token = tokens.FirstOrDefault(t => 
                string.Equals(t.Address, address, StringComparison.OrdinalIgnoreCase));

            if (token == null)
            {
                _logger.LogWarning("Token with address {Address} not found in verified tokens", address);
                return ApiResult<JupiterToken?>.Error(ErrorType.Unknown, "404", "Token not found in verified tokens list");
            }

            _logger.LogInformation("Successfully retrieved verified token: {Symbol} ({Address})", token.Symbol, token.Address);

            return ApiResult<JupiterToken?>.Ok(token);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Failed to fetch verified token from Jupiter API");
            return ApiResult<JupiterToken?>.Error(ErrorType.Unknown, null, ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while fetching verified token");
            return ApiResult<JupiterToken?>.Error(ErrorType.Unknown, null, "An unexpected error occurred while fetching verified token");
        }
    }

    /// <summary>
    /// Gets current prices for multiple tokens
    /// </summary>
    /// <param name="tokenAddresses">Comma-separated list of token addresses</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Dictionary of token addresses and their price data</returns>
    [HttpGet("prices")]
    public async Task<ApiResult<JupiterPriceResponse>> GetTokenPricesAsync(
        [FromQuery] string tokenAddresses,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(tokenAddresses))
            {
                return ApiResult<JupiterPriceResponse>.Error(ErrorType.Unknown, "INVALID_INPUT", "Token addresses parameter is required");
            }

            var addresses = tokenAddresses.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(addr => addr.Trim())
                .Where(addr => !string.IsNullOrWhiteSpace(addr))
                .ToArray();

            if (addresses.Length == 0)
            {
                return ApiResult<JupiterPriceResponse>.Error(ErrorType.Unknown, "INVALID_INPUT", "At least one valid token address is required");
            }

            var prices = await _jupiterTokenService.GetTokenPricesAsync(addresses, cancellationToken);
            return ApiResult<JupiterPriceResponse>.Ok(prices);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Failed to fetch token prices from Jupiter API");
            return ApiResult<JupiterPriceResponse>.Error(ErrorType.Unknown, "JUPITER_API_ERROR", ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while fetching token prices");
            return ApiResult<JupiterPriceResponse>.Error(ErrorType.Unknown, null, "An unexpected error occurred while fetching token prices");
        }
    }

    /// <summary>
    /// Gets current price for a single token
    /// </summary>
    /// <param name="tokenAddress">Token address to get price for</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Price data for the token</returns>
    [HttpGet("prices/{tokenAddress}")]
    public async Task<ApiResult<JupiterTokenPrice?>> GetTokenPriceAsync(
        string tokenAddress,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(tokenAddress))
            {
                return ApiResult<JupiterTokenPrice?>.Error(ErrorType.Unknown, "INVALID_INPUT", "Token address is required");
            }

            var price = await _jupiterTokenService.GetTokenPriceAsync(tokenAddress, cancellationToken);
            return ApiResult<JupiterTokenPrice?>.Ok(price);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Failed to fetch token price from Jupiter API for token: {TokenAddress}", tokenAddress);
            return ApiResult<JupiterTokenPrice?>.Error(ErrorType.Unknown, "JUPITER_API_ERROR", ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while fetching token price for token: {TokenAddress}", tokenAddress);
            return ApiResult<JupiterTokenPrice?>.Error(ErrorType.Unknown, null, "An unexpected error occurred while fetching token price");
        }
    }

    /// <summary>
    /// Gets a swap quote with route information from Jupiter
    /// </summary>
    /// <param name="inputMint">Input token mint address</param>
    /// <param name="outputMint">Output token mint address</param>
    /// <param name="amount">Amount to swap (in smallest unit)</param>
    /// <param name="slippageBps">Slippage in basis points (default: 50 = 0.5%)</param>
    /// <returns>Swap quote with route information</returns>
    [HttpGet("swap-quote")]
    [ProducesResponseType(typeof(ApiResult<JupiterSwapQuote?>), 200)]
    [ProducesResponseType(typeof(ApiResult<JupiterSwapQuote?>), 400)]
    [ProducesResponseType(typeof(ApiResult<JupiterSwapQuote?>), 500)]
    public async Task<ApiResult<JupiterSwapQuote?>> GetSwapQuote(
        [FromQuery] string inputMint,
        [FromQuery] string outputMint,
        [FromQuery] string amount,
        [FromQuery] int slippageBps = 50)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(inputMint))
            {
                return ApiResult<JupiterSwapQuote?>.Error(ErrorType.Unknown, "INVALID_INPUT_MINT", "Input mint address is required");
            }

            if (string.IsNullOrWhiteSpace(outputMint))
            {
                return ApiResult<JupiterSwapQuote?>.Error(ErrorType.Unknown, "INVALID_OUTPUT_MINT", "Output mint address is required");
            }

            if (string.IsNullOrWhiteSpace(amount))
            {
                return ApiResult<JupiterSwapQuote?>.Error(ErrorType.Unknown, "INVALID_AMOUNT", "Amount is required");
            }

            if (slippageBps < 0 || slippageBps > 10000)
            {
                return ApiResult<JupiterSwapQuote?>.Error(ErrorType.Unknown, "INVALID_SLIPPAGE", "Slippage must be between 0 and 10000 basis points");
            }

            _logger.LogInformation("Fetching swap quote from Jupiter API: {InputMint} -> {OutputMint}, Amount: {Amount}, Slippage: {SlippageBps}bps", 
                inputMint, outputMint, amount, slippageBps);

            var quote = await _jupiterTokenService.GetSwapQuoteAsync(inputMint, outputMint, amount, slippageBps);

            if (quote == null)
            {
                _logger.LogWarning("No swap quote found for {InputMint} -> {OutputMint}", inputMint, outputMint);
                return ApiResult<JupiterSwapQuote?>.Error(ErrorType.Unknown, "NO_QUOTE_FOUND", "No swap quote found for the specified tokens and amount");
            }

            _logger.LogInformation("Successfully retrieved swap quote: {InputMint} -> {OutputMint}, Output: {OutAmount}, Routes: {RouteCount}", 
                inputMint, outputMint, quote.OutAmount, quote.RoutePlan.Length);

            return ApiResult<JupiterSwapQuote?>.Ok(quote);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Failed to fetch swap quote from Jupiter API: {InputMint} -> {OutputMint}", inputMint, outputMint);
            return ApiResult<JupiterSwapQuote?>.Error(ErrorType.Unknown, "JUPITER_API_ERROR", ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while fetching swap quote: {InputMint} -> {OutputMint}", inputMint, outputMint);
            return ApiResult<JupiterSwapQuote?>.Error(ErrorType.Unknown, null, "An unexpected error occurred while fetching swap quote");
        }
    }
}
