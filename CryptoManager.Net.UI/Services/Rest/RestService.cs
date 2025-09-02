using CryptoManager.Net.UI.Authorization;
using System.Net.Http.Json;
using CryptoManager.Net.UI.Models;
using MudBlazor;
using CryptoManager.Net.UI.Models.ApiModels.Response;
using System.Text.Json;
using System.Text.Json.Serialization;
using CryptoExchange.Net.Requests;
using System.IO;
using CryptoExchange.Net.Interfaces;
using System;
using CryptoExchange.Net.Objects.Errors;

namespace CryptoManager.Net.UI.Services.Rest
{
    public abstract class RestService
    {
        protected readonly ISnackbar _snackbar;
        protected readonly HttpClient _httpClient;
        protected readonly AuthStateProvider _authStateProvider;
        protected readonly JsonSerializerOptions _jsonOptions;

        private static Task<ApiUser?>? _refreshTask;

        public RestService(AuthStateProvider authStateProvider, HttpClient httpClient, ISnackbar snackbar)
        {
            _httpClient = httpClient;
            _authStateProvider = authStateProvider;
            _snackbar = snackbar;
            _jsonOptions = new JsonSerializerOptions()
            {
                PropertyNameCaseInsensitive = true
            };
            _jsonOptions.Converters.Add(new JsonStringEnumConverter());
        }

        public async Task GetPageAsync<T>(string path,
            Func<Page<T>, Task> onSuccess,
            Func<ApiError, Task> onError,
            bool authenticated,
            Dictionary<string, string>? parameters = null)
        {
            var uri = path + (parameters?.Count > 0 == true ? "?" + string.Join("&", parameters.Select(x => x.Key + "=" + x.Value)) : "");
            var request = new HttpRequestMessage(HttpMethod.Get, uri);
            if (authenticated)
            {
                var authToken = await _authStateProvider.GetAccessTokenAsync();
                if (authToken == null)
                {
                    await onError(new ApiError { ErrorType = ErrorType.Unauthorized, Message = "Unauthorized" });
                    return;
                }

                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", authToken);
            }

            try
            {
                var result = await _httpClient.SendAsync(request);
                ApiResultPaged<IEnumerable<T>>? data = null;
                if (result.IsSuccessStatusCode)
                    data = await result.Content.ReadFromJsonAsync<ApiResultPaged<IEnumerable<T>>>(_jsonOptions);

                if (result.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    // Token might be expired
                    var user = await TryRefreshTokenAsync();
                    if (user != null)
                    {
                        request = new HttpRequestMessage(HttpMethod.Get, uri);
                        var authToken = await _authStateProvider.GetAccessTokenAsync();
                        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", authToken);

                        // Refreshed, try the request again
                        result = await _httpClient.SendAsync(request);
                        if (result.IsSuccessStatusCode)
                            data = await result.Content.ReadFromJsonAsync<ApiResultPaged<IEnumerable<T>>>(_jsonOptions);
                    }
                    else
                    {
                        await _authStateProvider.TokenInvalidAsync();
                    }
                }

                var error = ParseError(result, data);
                if (error == null)
                    await onSuccess(new Page<T>(data!.Data!, data!.Total!.Value));
                else
                    await onError(error);
            }
            catch (HttpRequestException)
            {
                await onError(new ApiError { ErrorType = ErrorType.NetworkError, Message = "Failed to get response" });
            }
            catch (Exception ex)
            {
                await onError(new ApiError { ErrorType = ErrorType.Unknown, Message = "Unknown exception: " + ex.Message });
            }
        }

        protected async Task<ApiUser?> TryRefreshTokenAsync()
        {
            try
            {
                if (_refreshTask != null)
                    return await _refreshTask;

                _refreshTask = Task.Run(async () =>
                {
                    var result = await _httpClient.PostAsync("users/refresh", null);
                    ApiResult<ApiUser>? data = null;
                    if (result.IsSuccessStatusCode)
                        data = await result.Content.ReadFromJsonAsync<ApiResult<ApiUser>>(_jsonOptions);

                    var error = ParseError(result, data);
                    if (error == null)
                    {
                        await _authStateProvider.AccessTokenRefreshedAsync(data!.Data!.Jwt);
                    }
                    return data?.Data;
                });

                var result = await _refreshTask;
                _refreshTask = null;
                return result;
            }
            catch (HttpRequestException)
            {
                return null;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public async Task GetAsync<T>(string path,
            Func<T, Task> onSuccess,
            Func<ApiError, Task> onError,
            bool authenticated,
            Dictionary<string, string>? parameters = null)
        {
            var uri = path + (parameters?.Count > 0 == true ? "?" + string.Join("&", parameters.Select(x => x.Key + "=" + x.Value)) : "");
            var request = new HttpRequestMessage(HttpMethod.Get, uri);
            if (authenticated)
            {
                var authToken = await _authStateProvider.GetAccessTokenAsync();
                if (authToken == null)
                {
                    await onError(new ApiError { ErrorType = ErrorType.Unauthorized, Message = "Unauthorized" });
                    return;
                }

                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", authToken);
            }

            try
            {
                var result = await _httpClient.SendAsync(request);
                ApiResult<T>? data = null;
                if (result.IsSuccessStatusCode)
                    data = await result.Content.ReadFromJsonAsync<ApiResult<T>>(_jsonOptions);

                if (result.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    // Token might be expired
                    var user = await TryRefreshTokenAsync();
                    if (user != null)
                    {
                        request = new HttpRequestMessage(HttpMethod.Get, uri);
                        var authToken = await _authStateProvider.GetAccessTokenAsync();
                        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", authToken);

                        // Refreshed, try the request again
                        result = await _httpClient.SendAsync(request);
                        if (result.IsSuccessStatusCode)
                            data = await result.Content.ReadFromJsonAsync<ApiResult<T>>(_jsonOptions);
                    }
                    else
                    {
                        await _authStateProvider.TokenInvalidAsync();
                    }
                }

                var error = ParseError(result, data);
                if (error == null)
                    await onSuccess(data!.Data!);
                else
                    await onError(error);
            }
            catch (HttpRequestException)
            {
                await onError(new ApiError { ErrorType = ErrorType.NetworkError, Message = "Failed to get response" });
            }
            catch (Exception ex)
            {
                await onError(new ApiError { ErrorType = ErrorType.Unknown, Message = "Unknown exception: " + ex.Message });
            }
        }

        public async Task PostAsync(string path,
            Func<Task> onSuccess,
            Func<ApiError, Task> onError,
            bool authenticated,
            object? body = null)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, path);
            if (body != null)
                request.Content = JsonContent.Create(body);

            if (authenticated)
            {
                var authToken = await _authStateProvider.GetAccessTokenAsync();
                if (authToken == null)
                {
                    await onError(new ApiError { ErrorType = ErrorType.Unauthorized, Message = "Unauthorized" });
                    return;
                }

                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", authToken);
            }

            try
            {
                var result = await _httpClient.SendAsync(request);
                ApiResult? data = null;
                if (result.IsSuccessStatusCode)
                    data = await result.Content.ReadFromJsonAsync<ApiResult>(_jsonOptions);

                if (result.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    // Token might be expired
                    var user = await TryRefreshTokenAsync();
                    if (user != null)
                    {
                        request = new HttpRequestMessage(HttpMethod.Post, path);
                        if (body != null)
                            request.Content = JsonContent.Create(body);

                        var authToken = await _authStateProvider.GetAccessTokenAsync();
                        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", authToken);

                        // Refreshed, try the request again
                        result = await _httpClient.SendAsync(request);
                        if (result.IsSuccessStatusCode)
                            data = await result.Content.ReadFromJsonAsync<ApiResult>(_jsonOptions);
                    }
                    else
                    {
                        await _authStateProvider.TokenInvalidAsync();
                    }
                }

                var error = ParseError(result, data);
                if (error == null)
                    await onSuccess();
                else
                    await onError(error);
            }
            catch (HttpRequestException)
            {
                await onError(new ApiError { ErrorType = ErrorType.NetworkError, Message = "Failed to get response" });
            }
            catch (Exception ex)
            {
                await onError(new ApiError { ErrorType = ErrorType.Unknown, Message = "Unknown exception: " + ex.Message });
            }
        }

        public async Task PostAsync<T>(string path,
            Func<T, Task> onSuccess,
            Func<ApiError, Task> onError,
            bool authenticated,
            object? body = null)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, path);
            if (body != null)
                request.Content = JsonContent.Create(body);

            if (authenticated)
            {
                var authToken = await _authStateProvider.GetAccessTokenAsync();
                if (authToken == null)
                {
                    await onError(new ApiError { ErrorType = ErrorType.Unauthorized, Message = "Unauthorized" });
                    return;
                }

                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", authToken);
            }

            try
            {
                var result = await _httpClient.SendAsync(request);
                ApiResult<T>? data = null;
                if (result.IsSuccessStatusCode)
                    data = await result.Content.ReadFromJsonAsync<ApiResult<T>>(_jsonOptions);

                if (result.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    // Token might be expired
                    var user = await TryRefreshTokenAsync();
                    if (user != null)
                    {
                        var authToken = await _authStateProvider.GetAccessTokenAsync();
                        request = new HttpRequestMessage(HttpMethod.Post, path);
                        if (body != null)
                            request.Content = JsonContent.Create(body);
                        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", authToken);

                        // Refreshed, try the request again
                        result = await _httpClient.SendAsync(request);
                        if (result.IsSuccessStatusCode)
                            data = await result.Content.ReadFromJsonAsync<ApiResult<T>>(_jsonOptions);
                    }
                    else
                    {
                        await _authStateProvider.TokenInvalidAsync();
                    }
                }

                var error = ParseError(result, data);
                if (error == null)
                    await onSuccess(data!.Data!);
                else
                    await onError(error);
            }
            catch (HttpRequestException)
            {
                await onError(new ApiError { ErrorType = ErrorType.NetworkError, Message = "Failed to get response" });
            }
            catch (Exception ex)
            {
                await onError(new ApiError { ErrorType = ErrorType.Unknown, Message = "Unknown exception: " + ex.Message });
            }
        }

        public async Task DeleteAsync(string path,
            Func<Task> onSuccess,
            Func<ApiError, Task> onError,
            bool authenticated,
            Dictionary<string, string>? parameters = null)
        {
            var uri = path + (parameters?.Count > 0 == true ? "?" + string.Join("&", parameters.Select(x => x.Key + "=" + x.Value)) : "");
            var request = new HttpRequestMessage(HttpMethod.Delete, uri);

            if (authenticated)
            {
                var authToken = await _authStateProvider.GetAccessTokenAsync();
                if (authToken == null)
                {
                    await onError(new ApiError { ErrorType = ErrorType.Unauthorized, Message = "Unauthorized" });
                    return;
                }

                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", authToken);
            }

            try
            {
                var result = await _httpClient.SendAsync(request);
                ApiResult? data = null;
                if (result.IsSuccessStatusCode)
                    data = await result.Content.ReadFromJsonAsync<ApiResult>(_jsonOptions);

                if (result.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    // Token might be expired
                    var user = await TryRefreshTokenAsync();
                    if (user != null)
                    {
                        request = new HttpRequestMessage(HttpMethod.Delete, uri);
                        var authToken = await _authStateProvider.GetAccessTokenAsync();
                        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", authToken);

                        // Refreshed, try the request again
                        result = await _httpClient.SendAsync(request);
                        if (result.IsSuccessStatusCode)
                            data = await result.Content.ReadFromJsonAsync<ApiResult>(_jsonOptions);
                    }
                    else
                    {
                        await _authStateProvider.TokenInvalidAsync();
                    }
                }

                var error = ParseError(result, data);
                if (error == null)
                    await onSuccess();
                else
                    await onError(error);
            }
            catch (HttpRequestException)
            {
                await onError(new ApiError { ErrorType = ErrorType.NetworkError, Message = "Failed to get response" });
            }
            catch (Exception ex)
            {
                await onError(new ApiError { ErrorType = ErrorType.Unknown, Message = "Unknown exception: " + ex.Message });
            }
        }

        public async Task DeleteAsync<T>(string path,
            Func<T, Task> onSuccess,
            Func<ApiError, Task> onError,
            bool authenticated,
            Dictionary<string, string>? parameters = null)
        {
            var uri = path + (parameters?.Count > 0 == true ? "?" + string.Join("&", parameters.Select(x => x.Key + "=" + x.Value)) : "");
            var request = new HttpRequestMessage(HttpMethod.Delete, path);

            if (authenticated)
            {
                var authToken = await _authStateProvider.GetAccessTokenAsync();
                if (authToken == null)
                {
                    await onError(new ApiError { ErrorType = ErrorType.Unauthorized, Message = "Unauthorized" });
                    return;
                }

                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", authToken);
            }

            try
            {
                var result = await _httpClient.SendAsync(request);
                ApiResult<T>? data = null;
                if (result.IsSuccessStatusCode)
                    data = await result.Content.ReadFromJsonAsync<ApiResult<T>>(_jsonOptions);

                if (result.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    // Token might be expired
                    var user = await TryRefreshTokenAsync();
                    if (user != null)
                    {
                        request = new HttpRequestMessage(HttpMethod.Delete, path);
                        var authToken = await _authStateProvider.GetAccessTokenAsync();
                        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", authToken);

                        // Refreshed, try the request again
                        result = await _httpClient.SendAsync(request);
                        if (result.IsSuccessStatusCode)
                            data = await result.Content.ReadFromJsonAsync<ApiResult<T>>(_jsonOptions);
                    }
                    else
                    {
                        await _authStateProvider.TokenInvalidAsync();
                    }
                }

                var error = ParseError(result, data);
                if (error == null)
                    await onSuccess(data!.Data!);
                else
                    await onError(error);
            }
            catch (HttpRequestException)
            {
                await onError(new ApiError { ErrorType = ErrorType.NetworkError, Message = "Failed to get response" });
            }
            catch (Exception ex)
            {
                await onError(new ApiError { ErrorType = ErrorType.Unknown, Message = "Unknown exception: " + ex.Message });
            }
        }

        private ApiError? ParseError(HttpResponseMessage responseMessage, ApiResult? result)
        {
            if (responseMessage.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                return new ApiError { ErrorType = ErrorType.Unauthorized, Message = "Unauthorized" };

            if (responseMessage.StatusCode == System.Net.HttpStatusCode.BadRequest)
                return new ApiError { ErrorType = ErrorType.InvalidParameter, Message = "Request error" };

            if (responseMessage.StatusCode == System.Net.HttpStatusCode.InternalServerError)
                return new ApiError { ErrorType = ErrorType.SystemError, Message = "Internal server error" };

            if (result == null)
                return new ApiError { ErrorType = ErrorType.Unknown, Message = "Null response" };

            if (result.Success)
                return null;

            var error = result.Errors.First();
            return error;
        }


        protected Task NotifyError(string topic, ApiError error)
        {
            if (IsIgnorableException(error))
                return Task.CompletedTask;

            var errorMessage = $" {error.ErrorType} | {topic} |";
            if (error.Code != null)
                errorMessage += $" {error.Code}";
            errorMessage += $" {error.Message}";

            _snackbar.Add(errorMessage, Severity.Warning);
            return Task.CompletedTask;
        }

        protected bool IsIgnorableException(ApiError error)
        {
            if (error.ErrorType == ErrorType.Unauthorized) // Unauthorized, need to login again
                return true;

            if (error.Code == "-10001") // No API Key configured
                return true;

            return false;
        }
    }
}
