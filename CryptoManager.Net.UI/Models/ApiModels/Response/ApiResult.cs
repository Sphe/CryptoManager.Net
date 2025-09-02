using CryptoExchange.Net.Objects.Errors;
using System.Text.Json.Serialization;

namespace CryptoManager.Net.UI.Models.ApiModels.Response
{
    public class ApiResult
    {
        public bool Success { get; set; }
        public IEnumerable<ApiError> Errors { get; set; } = [];
    }

    public class ApiResult<T> : ApiResult
    {
        public T? Data { get; set; }
    }

    public class ApiResultPaged<T> : ApiResult<T>
    {
        public int? Page { get; set; }
        public int? PageSize { get; set; }
        public int? Total { get; set; }
    }

    public class ApiError
    {
        [JsonPropertyName("type")]
        public ErrorType ErrorType { get; set; }
        public string? Code { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
