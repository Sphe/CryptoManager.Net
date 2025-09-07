using CryptoClients.Net;
using CryptoClients.Net.Interfaces;
using CryptoExchange.Net.Objects;
using CryptoExchange.Net.Objects.Errors;
using CryptoExchange.Net.SharedApis;
using CryptoManager.Net.ApiModels.Requests;
using CryptoManager.Net.Database;
using CryptoManager.Net.Database.Models;
using CryptoManager.Net.Models.Response;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using System.Net;

namespace CryptoManager.Net.Controllers
{
    /// <summary>
    /// Controller for managing API keys for cryptocurrency exchanges
    /// </summary>
    [ApiController]
    [Route("[controller]")]
    public class ApiKeysController : ApiController
    {
        private readonly ILogger _logger;
        private readonly IExchangeUserClientProvider _clientProvider;

        public ApiKeysController(ILogger<ApiKeysController> logger, MongoTrackerContext dbContext, IExchangeUserClientProvider clientProvider): base(dbContext)
        {
            _logger = logger;
            _clientProvider = clientProvider;
        }

        /// <summary>
        /// Gets a paginated list of API keys for the authenticated user
        /// </summary>
        /// <param name="page">Page number (default: 1)</param>
        /// <param name="pageSize">Number of items per page (default: 10)</param>
        /// <returns>A paginated list of API keys</returns>
        /// <response code="200">Returns the list of API keys</response>
        /// <response code="401">If the user is not authenticated</response>
        [HttpGet]
        public async Task<ApiResultPaged<IEnumerable<ApiApiKey>>> ListAsync(int page = 1, int pageSize = 10)
        {
            var filter = Builders<UserApiKey>.Filter.Eq(x => x.UserId, UserId);
            var sort = Builders<UserApiKey>.Sort.Ascending(x => x.Exchange);

            var total = await _dbContext.UserApiKeys.CountDocumentsAsync(filter);
            var keys = await _dbContext.UserApiKeys
                .Find(filter)
                .Sort(sort)
                .Skip((page - 1) * pageSize)
                .Limit(pageSize)
                .ToListAsync();

            return ApiResultPaged<IEnumerable<ApiApiKey>>.Ok(page, pageSize, (int)total, keys.Select(x => new ApiApiKey
            {
                Id = x.Id,
                Exchange = x.Exchange,
                Environment = x.Environment!,
                Key = x.Key,
                Invalid = x.Invalid
            }));
        }

        [HttpGet("configured")]
        public async Task<ApiResult<string[]>> GetApiKeysConfiguredAsync()
        {
            var filter = Builders<UserApiKey>.Filter.Eq(x => x.UserId, UserId);
            var sort = Builders<UserApiKey>.Sort.Ascending(x => x.Exchange);

            var query = await _dbContext.UserApiKeys
                .Find(filter)
                .Sort(sort)
                .Project(x => x.Exchange)
                .ToListAsync();

            return ApiResult<string[]>.Ok(query.ToArray());
        }

        [HttpPost]
        public async Task<ApiResult<string>> AddAsync(AddApiKeyRequest request)
        {
            var filter = Builders<UserApiKey>.Filter.And(
                Builders<UserApiKey>.Filter.Eq(x => x.UserId, UserId),
                Builders<UserApiKey>.Filter.Eq(x => x.Exchange, request.Exchange)
            );
            var existingKey = await _dbContext.UserApiKeys.Find(filter).FirstOrDefaultAsync();
            if (existingKey != null)
            {
                _logger.LogDebug("Key already exists for user {User} on exchange {Exchange}", UserId, request.Exchange);
                return ApiResult<string>.Error(ErrorType.InvalidOperation, null, $"Key already exists for for exchange {request.Exchange}");
            }

            var secret = WebUtility.UrlDecode(request.Secret)
                .Replace(' ', '+') // + char gets decoded to space
                .Replace("\\n", "\n");
            var newKey = new UserApiKey
            {
                Id = Guid.NewGuid().ToString(),
                UserId = UserId,
                Exchange = request.Exchange,
                Environment = request.Environment ?? TradeEnvironmentNames.Live,
                Key = request.Key,
                Secret = secret,
                Pass = request.Pass
            };
            await _dbContext.UserApiKeys.InsertOneAsync(newKey);

            _clientProvider.ClearUserClients(UserId, request.Exchange);            
            return ApiResult<string>.Ok(newKey.Id);
        }

        [HttpPost("validate/{id}")]
        public async Task<ApiResult<bool>> ValidateAsync(string id)
        {
            var filter = Builders<UserApiKey>.Filter.And(
                Builders<UserApiKey>.Filter.Eq(x => x.UserId, UserId),
                Builders<UserApiKey>.Filter.Eq(x => x.Id, id)
            );
            var existingKey = await _dbContext.UserApiKeys.Find(filter).FirstOrDefaultAsync();
            if (existingKey == null)
            {
                _logger.LogDebug("Key {id} not found for user {User}", id, UserId);
                return ApiResult<bool>.Error(ErrorType.Unknown, "404", $"Key not found");
            }

            if (existingKey.Invalid)
            {
                _logger.LogDebug("Key {id} for user {User} already marked as invalid", id, UserId);
                return ApiResult<bool>.Error(new ApiError(ErrorType.Unauthorized, null, "Invalid credentials"));
            }

            var environments = new Dictionary<string, string?> { { existingKey.Exchange, existingKey.Environment } };
            var client = new ExchangeRestClient(options => options.ApiEnvironments = environments);
            client.SetApiCredentials(existingKey.Exchange, existingKey.Key, existingKey.Secret, existingKey.Pass);

            var balanceClient = client.GetBalancesClient(TradingMode.Spot, existingKey.Exchange);
            if (balanceClient == null)
                return ApiResult<bool>.Error(new ApiError(ErrorType.InvalidOperation, null, "Balance client not available"));

            var result = await balanceClient.GetBalancesAsync(new GetBalancesRequest());
            if (!result.Success)
            {
                var update = Builders<UserApiKey>.Update.Set(x => x.Invalid, true);
                await _dbContext.UserApiKeys.UpdateOneAsync(filter, update);
                return ApiResult<bool>.Error(new ApiError(ErrorType.Unauthorized, null, "Invalid credentials"));
            }

            return ApiResult<bool>.Ok(result.Success);
        }

        [HttpPost("validate")]
        public async Task<ApiResult<bool>> ValidateAsync(string exchange, string environment, string apiKey, string apiSecret, string? apiPass = null)
        {
            apiSecret = WebUtility.UrlDecode(apiSecret)
                .Replace(' ', '+') // + char gets decoded to space
                .Replace("\\n", "\n"); 
            var client = new ExchangeRestClient(options => options.ApiEnvironments = new Dictionary<string, string?> { { exchange, environment } });
            try
            {
                client.SetApiCredentials(exchange, apiKey, apiSecret, apiPass);
            }
            catch (ArgumentException aex)
            {
                if (aex.ParamName == "apiPass")
                    return ApiResult<bool>.Error(ApiErrors.PassphraseRequired);

                return ApiResult<bool>.Error(new ApiError(ErrorType.Unauthorized, null, "Invalid credentials"));
            }
            catch (Exception)
            {
                return ApiResult<bool>.Error(new ApiError(ErrorType.Unauthorized, null, "Invalid credentials"));
            }

            var balanceClient = client.GetBalancesClient(TradingMode.Spot, exchange);
            if (balanceClient == null)
                return ApiResult<bool>.Error(new ApiError(ErrorType.InvalidOperation, null, "Balance client not available"));

            var result = await balanceClient.GetBalancesAsync(new GetBalancesRequest());
            if (!result.Success)
                return ApiResult<bool>.Error(new ApiError(ErrorType.Unauthorized, null, "Invalid credentials"));

            return ApiResult<bool>.Ok(result.Success);
        }

        [HttpDelete("{id}")]
        public async Task<ApiResult> DeleteAsync(string id)
        {
            var filter = Builders<UserApiKey>.Filter.And(
                Builders<UserApiKey>.Filter.Eq(x => x.UserId, UserId),
                Builders<UserApiKey>.Filter.Eq(x => x.Id, id)
            );
            var result = await _dbContext.UserApiKeys.DeleteOneAsync(filter);
            
            return ApiResult.Ok();
        }
    }
}
