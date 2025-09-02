using System.Net.Http.Json;
using CryptoManager.Net.UI.Authorization;
using CryptoManager.Net.UI.Models.ApiModels.Response;
using MudBlazor;

namespace CryptoManager.Net.UI.Services.Rest
{
    public class LoginService : RestService
    {
        public LoginService(AuthStateProvider authProvider, HttpClient httpClient, ISnackbar snackbar) 
            : base(authProvider, httpClient, snackbar)
        {
        }

        public async Task<ApiResult<ApiUser>> LoginAsync(string email, string password)
        {
            var uri = "users/login";
            try
            {
                var request = new HttpRequestMessage();
                var result = await _httpClient.PostAsJsonAsync(uri, new Dictionary<string, string> { { "email", email }, { "password", password } });
                if (!result.IsSuccessStatusCode)
                    return new ApiResult<ApiUser> { Success = false, Errors = [new ApiError { Message = "Login failed" }] };

                var data = await result.Content.ReadFromJsonAsync<ApiResult<ApiUser>>();
                if (!data!.Success)
                    return new ApiResult<ApiUser> { Success = false, Errors = data.Errors };

                await _authStateProvider.LoginAsync(data.Data!);
                return new ApiResult<ApiUser> { Success = true, Data = data.Data };
            }
            catch (Exception ex)
            {
                return new ApiResult<ApiUser> { Success = false, Errors = [new ApiError { Message = ex.Message }] };
            }
        }

        public async Task<ApiResult<ApiUser>> RegisterAsync(string email, string password)
        {
            var uri = "users/register";
            try
            {
                var request = new HttpRequestMessage();
                var result = await _httpClient.PostAsJsonAsync(uri, new Dictionary<string, string> { { "email", email }, { "password", password } });
                if (!result.IsSuccessStatusCode)
                    return new ApiResult<ApiUser> { Success = false, Errors = [new ApiError { Message = "Registration failed" }] };

                var data = await result.Content.ReadFromJsonAsync<ApiResult<ApiUser>>();
                if (!data!.Success)
                    return new ApiResult<ApiUser> { Success = false, Errors = data.Errors };

                await _authStateProvider.LoginAsync(data.Data!);
                return new ApiResult<ApiUser> { Success = true, Data = data.Data };
            }
            catch (Exception ex)
            {
                return new ApiResult<ApiUser> { Success = false, Errors = [new ApiError { Message = ex.Message }] };
            }
        }

        public async Task<ApiResult> RefreshUserAsync()
        {
            try
            {
                var userToken = await _authStateProvider.GetAccessTokenAsync();
                if (userToken == null)
                    return new ApiResult() { Success = true };

                var request = new HttpRequestMessage(HttpMethod.Get, "users");
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", userToken);

                var result = await _httpClient.SendAsync(request);

                ApiResult<ApiUser>? data = null;
                if (result.IsSuccessStatusCode)
                {
                    data = await result.Content.ReadFromJsonAsync<ApiResult<ApiUser>>(_jsonOptions);
                    if (data!.Success)
                        _authStateProvider.UpdateUser(data.Data!);
                }

                if (result.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    // Token might be expired
                    var user = await TryRefreshTokenAsync();
                    if (user != null)
                    {
                        request = new HttpRequestMessage(HttpMethod.Get, "users");
                        var authToken = await _authStateProvider.GetAccessTokenAsync();
                        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", authToken);

                        // Refreshed, try the request again
                        result = await _httpClient.SendAsync(request);
                        if (result.IsSuccessStatusCode)
                        {
                            data = await result.Content.ReadFromJsonAsync<ApiResult<ApiUser>>(_jsonOptions);
                            _authStateProvider.UpdateUser(data!.Data!);
                        }
                    }
                    else
                    {
                        await _authStateProvider.TokenInvalidAsync();
                    }
                }

                if (!result.IsSuccessStatusCode)
                    return new ApiResult { Success = false, Errors = [new ApiError { Message = "Unable to get user status" }] };

                if (!data!.Success)
                    return new ApiResult { Success = false, Errors = data.Errors };

                return new ApiResult { Success = true };
            }
            catch (Exception ex)
            {
                return new ApiResult { Success = false, Errors = [new ApiError { Message = ex.Message }] };
            }
        }

        public async Task LogoutAsync()
        {
            try
            {
                await _httpClient.PostAsync("users/logout", null);
            }
            catch (Exception)
            {
            }

            await _authStateProvider.LogoutAsync();
        }
    }
}
