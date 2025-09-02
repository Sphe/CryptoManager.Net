using MudBlazor;
using CryptoManager.Net.UI.Authorization;
using CryptoManager.Net.UI.Models;
using CryptoManager.Net.UI.Models.ApiModels.Response;
using CryptoManager.Net.UI.Models.ApiModels.Requests;

namespace CryptoManager.Net.UI.Services.Rest
{
    public class OrderService : RestService
    {
        public OrderService(AuthStateProvider authStateProvider, HttpClient httpClient, ISnackbar snackbar)
            : base(authStateProvider, httpClient, snackbar)
        {
        }

        public async Task UpdateClosedOrdersAsync(
            Func<Task> onSuccess,
            Func<ApiError, Task>? onError,
            string? exchange,
            string baseAsset,
            string quoteAsset)
        {
            await PostAsync($"orders/update/closed?exchange={exchange}&baseAsset={baseAsset}&quoteAsset={quoteAsset}", onSuccess, onError ?? (x => NotifyError("Update Orders", x)), true);
        }

        public async Task UpdateOpenOrdersAsync(
            Func<Task> onSuccess,
            Func<ApiError, Task>? onError,
            string? symbolId = null)
        {
            await PostAsync($"orders/update/open?symbolId={symbolId}", onSuccess, onError ?? (x => NotifyError("Update Orders", x)), true);
        }

        public async Task UpdateUserTradesAsync(
            Func<Page<ApiBalance>, Task> onSuccess,
            Func<ApiError, Task>? onError,
            string symbolId)
        {
            await PostAsync($"trades/update?symbolId={symbolId}", onSuccess, onError ?? (x => NotifyError("Update User Trades", x)), true);
        }

        public async Task PlaceOrderAsync(
            Func<string, Task> onSuccess,
            Func<ApiError, Task>? onError,
            PlaceOrderRequest request)
        {
            await PostAsync($"orders", onSuccess, onError ?? (x => NotifyError("Place order", x)), true, request);
        }

        public async Task CancelOrderAsync(
            Func<Task> onSuccess,
            Func<ApiError, Task>? onError,
            string orderId)
        {
            await DeleteAsync($"orders/{orderId}", onSuccess, onError ?? (x => NotifyError("Cancel order", x)), true);
        }

        public async Task GetOrderAsync(
            Func<ApiOrder, Task> onSuccess,
            Func<ApiError, Task>? onError,
            string orderId)
        {
            await GetAsync("orders/" + orderId, onSuccess, onError ?? (x => NotifyError("Get Order Info", x)), true);
        }

        public async Task GetOpenOrdersAsync(
            Func<Page<ApiOrder>, Task> onSuccess,
            Func<ApiError, Task>? onError,
            string? exchange = null,
            string? baseAsset = null,
            string? quoteAsset = null,
            string? sort = null,
            string? sortDirection = null,
            int? page = null,
            int? pageSize = null)
        {
            var parameters = new Dictionary<string, string>();
            if (!string.IsNullOrEmpty(exchange))
                parameters.Add("exchange", exchange);
            if (baseAsset != null)
                parameters.Add("baseAsset", baseAsset);
            if (quoteAsset != null)
                parameters.Add("quoteAsset", quoteAsset);
            if (!string.IsNullOrEmpty(sort))
                parameters.Add("order", sort);
            if (!string.IsNullOrEmpty(sortDirection))
                parameters.Add("orderDirection", sortDirection);
            if (page != null)
                parameters.Add("page", page.ToString()!);
            if (pageSize != null)
                parameters.Add("pageSize", pageSize.ToString()!);

            await GetPageAsync("orders/open", onSuccess, onError ?? (x => NotifyError("Get Open Orders", x)), true, parameters);
        }

        public async Task GetClosedOrdersAsync(
            Func<Page<ApiOrder>, Task> onSuccess,
            Func<ApiError, Task>? onError,
            string? exchange = null,
            string? baseAsset = null,
            string? quoteAsset = null,
            string? sort = null,
            string? sortDirection = null,
            int? page = null,
            int? pageSize = null)
        {
            var parameters = new Dictionary<string, string>();
            if (baseAsset != null)
                parameters.Add("baseAsset", baseAsset.ToString()!);
            if (quoteAsset != null)
                parameters.Add("quoteAsset", quoteAsset.ToString()!);
            if (!string.IsNullOrEmpty(exchange))
                parameters.Add("exchange", exchange);
            if (!string.IsNullOrEmpty(sort))
                parameters.Add("orderBy", sort);
            if (!string.IsNullOrEmpty(sortDirection))
                parameters.Add("orderDirection", sortDirection);
            if (page != null)
                parameters.Add("page", page.ToString()!);
            if (pageSize != null)
                parameters.Add("pageSize", pageSize.ToString()!);

            await GetPageAsync("orders/closed", onSuccess, onError ?? (x => NotifyError("Get Closed Orders", x)), true, parameters);
        }

        public async Task GetUserTradesAsync(
            Func<Page<ApiUserTrade>, Task> onSuccess,
            Func<ApiError, Task>? onError,
            string? symbolId = null,
            string? orderId = null,
            string? sort = null,
            string? sortDirection = null,
            int? page = null,
            int? pageSize = null)
        {
            var parameters = new Dictionary<string, string>();
            if (symbolId != null)
                parameters.Add("symbolId", symbolId.ToString()!);
            if (!string.IsNullOrEmpty(orderId))
                parameters.Add("orderId", orderId);
            if (!string.IsNullOrEmpty(sort))
                parameters.Add("orderBy", sort);
            if (!string.IsNullOrEmpty(sortDirection))
                parameters.Add("orderDirection", sortDirection);
            if (page != null)
                parameters.Add("page", page.ToString()!);
            if (pageSize != null)
                parameters.Add("pageSize", pageSize.ToString()!);

            await GetPageAsync("trades", onSuccess, onError ?? (x => NotifyError("Get User Trades", x)), true, parameters);
        }
    }
}
