using BlazorApplicationInsights.Interfaces;
using CryptoManager.Net.UI.Models.ApiModels.Response;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;
using System.Security.Claims;

namespace CryptoManager.Net.UI.Authorization
{
    public class AuthStateProvider : AuthenticationStateProvider
    {
        private DateTime _loginTime;
        private readonly IJSRuntime _jsRuntime;

        public ApiUser? User { get; private set; }

        public event Action? OnAccessTokenRefreshed;

        public AuthStateProvider(IJSRuntime jsRuntime)
        {
            _jsRuntime = jsRuntime;
        }

        public async Task LoginAsync(ApiUser user)
        {
            _loginTime = DateTime.UtcNow;
            User = user;

            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", new object[] { "jwt", user.Jwt });
            NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(GetUserClaimsPrincipal())));
        }

        public void UpdateUser(ApiUser user)
        {
            User = user;
            NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(GetUserClaimsPrincipal())));
        }

        public async Task LogoutAsync()
        {
            User = null;

            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "jwt");
            NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(new())));
        }

        public async Task TokenInvalidAsync()
        {
            if (DateTime.UtcNow - _loginTime < TimeSpan.FromMinutes(1))
                return;

            if (User == null)
                return;

            User = null;
            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "jwt");
            NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(new())));
        }

        public override Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            if (User == null)
                return Task.FromResult(new AuthenticationState(new ClaimsPrincipal()));

            return Task.FromResult(new AuthenticationState(GetUserClaimsPrincipal()));
        }

        public async Task AccessTokenRefreshedAsync(string token)
        {
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", new object[] { "jwt", token });
            OnAccessTokenRefreshed?.Invoke();
        }

        public async Task<string?> GetAccessTokenAsync()
        {
            return await _jsRuntime.InvokeAsync<string>("localStorage.getItem", "jwt");
        }

        private ClaimsPrincipal GetUserClaimsPrincipal() => new ClaimsPrincipal([new ClaimsIdentity([new Claim(ClaimTypes.Email, User!.Email)], "Custom")]);
    }
}
