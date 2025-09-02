using MudBlazor;
using System.Globalization;
using CryptoManager.Net.UI.Authorization;
using CryptoManager.Net.UI.Models;
using CryptoManager.Net.UI.Models.ApiModels.Response;
using CryptoManager.Net.UI.Models.ApiModels.Requests;

namespace CryptoManager.Net.UI.Services.Rest
{
    public class BalanceService : RestService
    {
        public BalanceService(AuthStateProvider authStateProvider, HttpClient httpClient, ISnackbar snackbar)
            : base(authStateProvider, httpClient, snackbar)
        {
        }

        public async Task UpdateBalancesAsync(
            Func<Task> onSuccess,
            Func<ApiError, Task>? onError,
            string? exchange = null)
        {
            await PostAsync($"balances/update?exchange={exchange}", onSuccess, onError ?? (x => NotifyError("Update Balances", x)), true);
        }

        public async Task GetBalancesAsync(
            Func<Page<ApiBalance>, Task> onSuccess,
            Func<ApiError, Task>? onError,
            string? query = null,
            string? exchange = null,
            string? asset = null,
            string? sort = null,
            string? sortDirection = null,
            int? page = null,
            int? pageSize = null)
        {
            var parameters = new Dictionary<string, string>();
            if (!string.IsNullOrEmpty(query))
                parameters.Add("query", query);
            if (!string.IsNullOrEmpty(exchange))
                parameters.Add("exchange", exchange);
            if (!string.IsNullOrEmpty(asset))
                parameters.Add("asset", asset);
            if (!string.IsNullOrEmpty(sort))
                parameters.Add("orderBy", sort);
            if (!string.IsNullOrEmpty(sortDirection))
                parameters.Add("orderDirection", sortDirection);
            if (page != null)
                parameters.Add("page", page.ToString()!);
            if (pageSize != null)
                parameters.Add("pageSize", pageSize.ToString()!);

            await GetPageAsync("balances/exchange", onSuccess, onError ?? (x => NotifyError("Get Balances", x)), true, parameters);
        }

        public async Task GetExternalBalancesAsync(
            Func<Page<ApiBalance>, Task> onSuccess,
            Func<ApiError, Task>? onError,
            string? query = null,
            string? asset = null,
            string? sort = null,
            string? sortDirection = null,
            int? page = null,
            int? pageSize = null)
        {
            var parameters = new Dictionary<string, string>();
            if (!string.IsNullOrEmpty(query))
                parameters.Add("query", query);
            if (!string.IsNullOrEmpty(asset))
                parameters.Add("asset", asset);
            if (!string.IsNullOrEmpty(sort))
                parameters.Add("orderBy", sort);
            if (!string.IsNullOrEmpty(sortDirection))
                parameters.Add("orderDirection", sortDirection);
            if (page != null)
                parameters.Add("page", page.ToString()!);
            if (pageSize != null)
                parameters.Add("pageSize", pageSize.ToString()!);

            await GetPageAsync("balances/external", onSuccess, onError ?? (x => NotifyError("Get External Balances", x)), true, parameters);
        }

        public async Task SetExternalBalanceAsync(
            Func<Task> onSuccess,
            Func<ApiError, Task>? onError,
            string asset,
            decimal quantity)
        {
            var uri = $"balances/external";
            await PostAsync(uri, onSuccess, onError ?? (x => NotifyError("Set External Balance", x)), true, new UpdateExternalBalanceRequest
            {
                Asset = asset,
                Total = quantity
            });
        }

        public async Task RemoveExternalBalanceAsync(
            Func<Task> onSuccess,
            Func<ApiError, Task>? onError,
            string balanceId)
        {
            var uri = $"balances/external/{balanceId}";
            await DeleteAsync(uri, onSuccess, onError ?? (x => NotifyError("Delete External Balance", x)), true);
        }

        public async Task GetAssetsAsync(
            Func<IEnumerable<BalanceAsset>, Task> onSuccess,
            Func<ApiError, Task>? onError)
        {
            // Not available currently
            await GetAsync("balances/assets", onSuccess, onError ?? (x => NotifyError("Get Asset Balances", x)), true, new Dictionary<string, string>());
        }

        public async Task GetValueAsync(
            Func<ApiBalanceValuation, Task> onSuccess,
            Func<ApiError, Task>? onError,
            string? exchange = null)
        {
            var parameters = new Dictionary<string, string>();
            if (!string.IsNullOrEmpty(exchange))
                parameters.Add("exchange", exchange);

            await GetAsync("balances/valuation", onSuccess, onError ?? (x => NotifyError("Get Portfolio Value", x)), true, parameters);
        }

        public async Task GetHistoryAsync(
            Func<AccountHistory[], Task> onSuccess,
            Func<ApiError, Task>? onError,
            string period)
        {
            // Not available currently
            await GetAsync("balances/history?period=" + period, onSuccess, onError ?? (x => NotifyError("Get Portfolio Value History", x)), true);
        }
    }
}
