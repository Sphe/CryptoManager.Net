using CryptoExchange.Net.Objects.Errors;
using CryptoManager.Net.Models.Response;

namespace CryptoManager.Net
{
    public static class ApiErrors
    {
        private const int _apiErrorCodeRangeStart = -10000;

        public static ApiError NoApiKeyConfigured { get; } = new ApiError(ErrorType.Unauthorized, (_apiErrorCodeRangeStart - 1).ToString(), "No API key configured for exchange");
        public static ApiError PassphraseRequired { get; } = new ApiError(ErrorType.Unauthorized, (_apiErrorCodeRangeStart - 2).ToString(), "Pass is required");
        public static ApiError RegistrationDisabled { get; } = new ApiError(ErrorType.Unknown, (_apiErrorCodeRangeStart - 3).ToString(), "Registration is not enabled");
        public static ApiError LoginFailed { get; } = new ApiError(ErrorType.Unauthorized, (_apiErrorCodeRangeStart - 4).ToString(), "Login failed");
        public static ApiError RefreshFailed { get; } = new ApiError(ErrorType.Unknown, (_apiErrorCodeRangeStart - 5).ToString(), "Refresh failed");
    }

}
