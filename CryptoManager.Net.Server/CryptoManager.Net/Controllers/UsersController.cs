using CryptoExchange.Net.Objects.Errors;
using CryptoManager.Net.ApiModels.Requests;
using CryptoManager.Net.Database;
using CryptoManager.Net.Database.Models;
using CryptoManager.Net.Models.Response;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

namespace CryptoManager.Net.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class UsersController : ApiController
    {
        private readonly ILogger _logger;
        private readonly IConfiguration _configuration;
        public UsersController(ILogger<UsersController> logger, IConfiguration configuration, MongoTrackerContext dbContext) : base(dbContext)         {
            _logger = logger;
            _configuration = configuration;
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public Task<ApiResult<ApiUser>> LoginAsync(LoginRequest request)
        {
            // MongoDB conversion and authentication services removed - returning error for now
            return Task.FromResult(ApiResult<ApiUser>.Error(ApiErrors.LoginFailed));
        }

        [HttpPost("refresh")]
        [AllowAnonymous]
        public Task<ApiResult<ApiUser>> RefreshAsync()
        {
            var refreshToken = Request.Cookies["refreshToken"];
            if (string.IsNullOrEmpty(refreshToken))
            {
                _logger.LogDebug("Refresh token not in request");
                return Task.FromResult(ApiResult<ApiUser>.Error(ApiErrors.RefreshFailed));
            }

            // MongoDB conversion and authentication services removed - returning error for now
            return Task.FromResult(ApiResult<ApiUser>.Error(ApiErrors.RefreshFailed));
        }

        [HttpPost("logout")]
        public Task<ApiResult> LogoutAsync()
        {
            var refreshToken = Request.Cookies["refreshToken"];
            if (string.IsNullOrEmpty(refreshToken))
            {
                _logger.LogDebug("Refresh token not in request");
                return Task.FromResult(ApiResult.Ok());
            }

            // Authentication services removed - returning success for now
            return Task.FromResult(ApiResult.Ok());
        }

        [HttpGet]
        [ResponseCache(NoStore = true)]
        public Task<ApiResult<ApiUser>> GetUserAsync()
        {
            // MongoDB conversion needed - returning error for now
            return Task.FromResult(ApiResult<ApiUser>.Error(ApiErrors.LoginFailed));
        }

        [HttpPost("register")]
        [AllowAnonymous]
        public Task<ApiResult<ApiUser>> RegisterAsync(RegisterRequest request)
        {
            if (!_configuration.GetValue<bool>("RegistrationEnabled"))
                return Task.FromResult(ApiResult<ApiUser>.Error(ApiErrors.RegistrationDisabled));

            if (request.Email.Length < 6)
                return Task.FromResult(ApiResult<ApiUser>.Error(ErrorType.Unknown, null, "Email too short"));

            if (request.Password.Length < 8)
                return Task.FromResult(ApiResult<ApiUser>.Error(ErrorType.Unknown, null, "Password too short"));

            // MongoDB conversion and authentication services removed - returning error for now
            return Task.FromResult(ApiResult<ApiUser>.Error(ApiErrors.RegistrationDisabled));
        }
    }
}
