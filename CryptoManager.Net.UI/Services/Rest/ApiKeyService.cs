using MudBlazor;
using CryptoManager.Net.UI.Authorization;
using CryptoManager.Net.UI.Models;
using CryptoManager.Net.UI.Models.ApiModels.Response;
using CryptoManager.Net.UI.Models.ApiModels.Requests;

namespace CryptoManager.Net.UI.Services.Rest
{
    public class ApiKeyService : RestService
    {

        public ApiKeyService(AuthStateProvider authStateProvider, HttpClient httpClient, ISnackbar snackbar) : base(authStateProvider, httpClient, snackbar)
        {
        }

        public async Task GetApiKeysAsync(
            Func<Page<ApiApiKey>, Task> onSuccess,
            Func<ApiError, Task>? onError,
            int? page = null,
            int? pageSize = null)
        {
            var parameters = new Dictionary<string, string>();
            if (page != null)
                parameters.Add("page", page.ToString()!);
            if (pageSize != null)
                parameters.Add("pageSize", pageSize.ToString()!);

            await GetPageAsync("apikeys", onSuccess, onError ?? (x => NotifyError("Get Api Keys", x)), true, parameters);
        }

        public async Task GetConfiguredExchangesAsync(
            Func<string[], Task> onSuccess,
            Func<ApiError, Task>? onError)
        {
            await GetAsync("apikeys/configured", onSuccess, onError ?? (x => NotifyError("Get Configured Api Keys", x)), true);
        }

        public async Task ValidateApiKeyAsync(
            Func<bool, Task> onSuccess,
            Func<ApiError, Task>? onError,
            int id)
        {
            await PostAsync("apikeys/validate/" + id, onSuccess, onError ?? (x => NotifyError("Validate Api Key", x)), true);
        }

        public async Task ValidateApiKeyAsync(
            Func<bool, Task> onSuccess,
            Func<ApiError, Task>? onError,
            string exchange,
            string environment,
            string apiKey,
            string apiSecret,
            string? apiPass)
        {
            await PostAsync($"apikeys/validate?exchange={exchange}&environment={environment}&apiKey={apiKey}&apiSecret={apiSecret}&apiPass={apiPass}", onSuccess, onError ?? (x => NotifyError("Validate Api Key", x)), true);
        }

        public async Task RemoveApiKeyAsync(
            Func<Task> onSuccess,
            Func<ApiError, Task>? onError,
            int id)
        {
            await DeleteAsync("apikeys/" + id, onSuccess, onError ?? (x => NotifyError("Delete Api Key", x)), true);
        }

        public async Task AddApiKeyAsync(
            Func<Task> onSuccess,
            Func<ApiError, Task>? onError,
            AddApiKeyRequest apiKey)
        {
            await PostAsync("apikeys", onSuccess, onError ?? (x => NotifyError("Add Api Key", x)), true, apiKey);
        }
    }
}
