using MudBlazor;
using CryptoManager.Net.UI.Authorization;
using CryptoManager.Net.UI.Models;
using CryptoManager.Net.UI.Models.ApiModels.Response;
using CryptoManager.Net.UI.Models.ApiModels.Requests;
using Microsoft.JSInterop;
using System.Text.Json.Serialization;
using System.Text.Json;

namespace CryptoManager.Net.UI.Services.Rest
{
    public class QuickViewService : RestService
    {
        private readonly IJSRuntime _jsRuntime;

        public QuickViewService(IJSRuntime jsRuntime, AuthStateProvider authStateProvider, HttpClient httpClient, ISnackbar snackbar) 
            : base(authStateProvider, httpClient, snackbar)
        {
            _jsRuntime = jsRuntime;
        }

        public async Task GetSymbolsAsync(
            Func<string[], Task> onSuccess,
            Func<ApiError, Task>? onError)
        {
            if (_authStateProvider.User == null)
            {
                // Use local storage
                var data = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", "quickview");
                await onSuccess(JsonSerializer.Deserialize<string[]>(data ?? "[]")!.Take(8).ToArray()!);
            }
            else
            {
                // Use API
                await GetAsync("quickview", onSuccess, onError ?? (x => NotifyError("Get Quick View Configuration", x)), true, null);
            }
        }

        public async Task RemoveSymbolAsync(
            Func<bool, Task> onSuccess,
            Func<ApiError, Task>? onError, 
            string symbol)
        {
            if (_authStateProvider.User == null)
            {
                // Use local storage
                var data = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", "quickview");
                var des = JsonSerializer.Deserialize<List<string>>(data ?? "[]");
                des!.Remove(symbol);
                data = JsonSerializer.Serialize(des);
                await _jsRuntime.InvokeVoidAsync("localStorage.setItem", new object[] { "quickview", data });
                await onSuccess(true);
            }
            else
            {
                // Use API
                await DeleteAsync("quickview/" + symbol, onSuccess, onError ?? (x => NotifyError("Remove QuickView symbol", x)), true, null);                
            }
        }

        public async Task AddSymbolAsync(
            Func<bool, Task> onSuccess,
            Func<ApiError, Task>? onError,
            string symbolId)
        {
            if (_authStateProvider.User == null)
            {
                // Use local storage
                var data = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", "quickview");
                var des = JsonSerializer.Deserialize<List<string>>(data ?? "[]")!;
                des.Add(symbolId);
                data = JsonSerializer.Serialize(des);
                await _jsRuntime.InvokeVoidAsync("localStorage.setItem", new object[] { "quickview", data });
            }
            else
            {
                // Use API
                await PostAsync("quickview", onSuccess, onError ?? (x => NotifyError("Remove QuickView symbol", x)), true, new Dictionary<string, object>
                {
                    { "Symbol", symbolId }
                });
            }
        }
    }
}
