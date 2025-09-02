using BlazorApplicationInsights;
using CryptoManager.Net.UI;
using CryptoManager.Net.UI.Authorization;
using CryptoManager.Net.UI.Services;
using CryptoManager.Net.UI.Services.Rest;
using CryptoManager.Net.UI.Services.Stream;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor;
using MudBlazor.Services;

var configFileName = Environment.GetEnvironmentVariable("environment") == "Local" ? "appsettings.Local.json" : "appsettings.json";
var configuration = new ConfigurationBuilder().AddJsonFile(configFileName).Build();

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddSingleton<IConfiguration>(configuration);

builder.Services.AddScoped(sp => new HttpClient(new CookieHandler()) { 
    BaseAddress = new Uri(configuration["ApiAddress"]!),
    Timeout = TimeSpan.FromSeconds(15)
});

builder.Services.AddScoped<SymbolService>();
builder.Services.AddScoped<ExchangeService>();
builder.Services.AddScoped<OrderService>();
builder.Services.AddScoped<BalanceService>();
builder.Services.AddScoped<ApiKeyService>();
builder.Services.AddScoped<QuickViewService>();

builder.Services.AddScoped<StreamService>();

builder.Services.AddScoped<LoginService>();
builder.Services.AddScoped<AuthStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(sp => sp.GetRequiredService<AuthStateProvider>());
builder.Services.AddAuthorizationCore();
builder.Services.AddCascadingAuthenticationState();

#if !DEBUG
builder.Services.AddBlazorApplicationInsights(x =>
{
    x.ConnectionString = configuration["AppInsights"]!;
});
#endif

builder.Services.AddMudServices(config => {

    config.SnackbarConfiguration.PositionClass = Defaults.Classes.Position.BottomLeft;
    config.SnackbarConfiguration.MaxDisplayedSnackbars = 5;
});

await builder.Build().RunAsync();
