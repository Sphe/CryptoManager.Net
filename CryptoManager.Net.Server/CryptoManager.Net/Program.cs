using CryptoManager.Net;
using CryptoManager.Net.Analyzers;
using CryptoManager.Net.Caching;
using CryptoManager.Net.Database;
using MongoDB.Driver;
using CryptoManager.Net.Models;
using CryptoManager.Net.Processor.FiatPrices;
using CryptoManager.Net.Processor.Symbols;
using CryptoManager.Net.Processor.Tickers;
using CryptoManager.Net.Processor.AssetCalculation;
using CryptoManager.Net.Publish;
using CryptoManager.Net.Publisher.FiatPrices;
using CryptoManager.Net.Publisher.Symbols;
using CryptoManager.Net.Publisher.Tickers;
using CryptoManager.Net.Services.External;
using CryptoManager.Net.Subscriptions.KLine;
using CryptoManager.Net.Subscriptions.OrderBook;
using CryptoManager.Net.Subscriptions.Tickers;
using CryptoManager.Net.Subscriptions.Trades;
using CryptoManager.Net.Subscriptions.User;
using CryptoManager.Net.Websockets;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers().AddJsonOptions(x =>
{
    x.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "CryptoManager.Net API",
        Version = "v1",
        Description = "A comprehensive cryptocurrency portfolio management API",
        Contact = new Microsoft.OpenApi.Models.OpenApiContact
        {
            Name = "CryptoManager.Net",
            Email = "support@cryptomanager.net"
        }
    });
    
    // Include XML comments if available
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = System.IO.Path.Combine(System.AppContext.BaseDirectory, xmlFile);
    if (System.IO.File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }
});
builder.Services.AddResponseCaching();

builder.Services.AddCryptoClients(options =>
{
    options.CachingEnabled = false;
});

builder.Services.AddSingleton<IPublishOutput<Symbol>, ChannelSymbolPublishOutput>(x => new ChannelSymbolPublishOutput(ChannelRegistrations.SymbolChannel.Writer));
builder.Services.AddSingleton<IPublishOutput<Ticker>, ChannelTickerPublishOutput>(x => new ChannelTickerPublishOutput(ChannelRegistrations.TickerChannel.Writer));
builder.Services.AddSingleton<IPublishOutput<FiatPrice>, ChannelFiatPricePublishOutput>(x => new ChannelFiatPricePublishOutput(ChannelRegistrations.FiatPriceChannel.Writer));
builder.Services.AddSingleton<IPublishOutput<PendingAssetCalculation>, ChannelAssetCalculationPublishOutput>(x => new ChannelAssetCalculationPublishOutput(ChannelRegistrations.AssetCalculationChannel.Writer));
builder.Services.AddSingleton<IPublishOutput<PendingSolanaAssetCalculation>, ChannelSolanaAssetCalculationPublishOutput>(x => new ChannelSolanaAssetCalculationPublishOutput(ChannelRegistrations.SolanaAssetCalculationChannel.Writer));
builder.Services.AddSingleton<IPublishOutput<PendingPoolPairCalculation>, ChannelPoolPairCalculationPublishOutput>(x => new ChannelPoolPairCalculationPublishOutput(ChannelRegistrations.PoolPairCalculationChannel.Writer));
builder.Services.AddSingleton<IProcessInput<Symbol>, ChannelSymbolProcessInput>(x => new ChannelSymbolProcessInput(ChannelRegistrations.SymbolChannel.Reader));
builder.Services.AddSingleton<IProcessInput<Ticker>, ChannelTickerProcessInput>(x => new ChannelTickerProcessInput(ChannelRegistrations.TickerChannel.Reader));
builder.Services.AddSingleton<IProcessInput<FiatPrice>, ChannelFiatPriceProcessInput>(x => new ChannelFiatPriceProcessInput(ChannelRegistrations.FiatPriceChannel.Reader));
builder.Services.AddSingleton<IProcessInput<PendingAssetCalculation>, ChannelAssetCalculationProcessInput>(x => new ChannelAssetCalculationProcessInput(ChannelRegistrations.AssetCalculationChannel.Reader));
builder.Services.AddSingleton<IProcessInput<PendingSolanaAssetCalculation>, ChannelSolanaAssetCalculationProcessInput>(x => new ChannelSolanaAssetCalculationProcessInput(ChannelRegistrations.SolanaAssetCalculationChannel.Reader));
builder.Services.AddSingleton<IProcessInput<PendingPoolPairCalculation>, ChannelPoolPairCalculationProcessInput>(x => new ChannelPoolPairCalculationProcessInput(ChannelRegistrations.PoolPairCalculationChannel.Reader));
builder.Services.AddAsSingletonAndBackgroundService<SymbolsProcessService>();
builder.Services.AddAsSingletonAndBackgroundService<SymbolPublishService>();
builder.Services.AddAsSingletonAndBackgroundService<TickerProcessService>();
builder.Services.AddAsSingletonAndBackgroundService<TickerPublishService>();
builder.Services.AddAsSingletonAndBackgroundService<FiatPricesProcessService>();
builder.Services.AddAsSingletonAndBackgroundService<FiatPricePublishService>();
builder.Services.AddAsSingletonAndBackgroundService<AssetCalculationProcessService>();
builder.Services.AddAsSingletonAndBackgroundService<AssetSolanaCalculationProcessService>();
builder.Services.AddAsSingletonAndBackgroundService<AssetAggregationService>();
builder.Services.AddAsSingletonAndBackgroundService<PoolPairCalculationProcessService>();
builder.Services.AddAsSingletonAndBackgroundService<PoolPairProcessService>();
builder.Services.AddAsSingletonAndBackgroundService<SymbolUsdVolumeService>();
builder.Services.AddAsSingletonAndBackgroundService<UserPortfolioSnapshotService>();
builder.Services.AddSingleton<KLineSubscriptionService>();
builder.Services.AddSingleton<TickerSubscriptionService>();
builder.Services.AddSingleton<TradeSubscriptionService>();
builder.Services.AddSingleton<OrderBookSubscriptionService>();
builder.Services.AddSingleton<UserSubscriptionService>();

builder.Services.AddTransient<CachingMiddleware>();
builder.Services.AddSingleton<ITrackerCache, TrackerCache>();

builder.Services.AddHostedService<MongoInitializationService>();
builder.Services.AddHostedService<BackgroundServiceManager>();
builder.Services.AddAsSingletonAndHostedService<WebsocketManager>();

// MongoDB configuration
var mongoConnectionString = builder.Configuration.GetConnectionString("MongoDB") ?? "mongodb://localhost:27017";
var mongoClient = new MongoClient(mongoConnectionString);
var mongoDatabase = mongoClient.GetDatabase("CryptoManager");

builder.Services.AddSingleton<IMongoDatabase>(mongoDatabase);
builder.Services.AddSingleton<IMongoDatabaseFactory>(provider => 
    new MongoDatabaseFactory(provider.GetRequiredService<IMongoDatabase>()));
builder.Services.AddScoped<MongoTrackerContext>(provider => 
    provider.GetRequiredService<IMongoDatabaseFactory>().CreateContext());

// Jupiter API services
builder.Services.AddHttpClient<IJupiterTokenService, JupiterTokenService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
});

// DexScreener API services
builder.Services.AddHttpClient<IDexScreenerService, DexScreenerService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
});

builder.Services.AddMemoryCache();

builder.Services.Configure<HostOptions>(options =>
{
    options.ServicesStartConcurrently = false;
});

builder.Services.AddCors(x =>
{
    x.AddPolicy("Default", x =>
    {
        x.WithOrigins(builder.Configuration.GetValue<string>("CorsOrigins")!.Split(';', StringSplitOptions.RemoveEmptyEntries));
        x.AllowAnyMethod();
        x.AllowAnyHeader();
        x.AllowCredentials();
    });
});

var app = builder.Build();

app.UseCors("Default");

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "CryptoManager.Net API v1");
        c.RoutePrefix = "swagger"; // Set Swagger UI at /swagger
        c.DocumentTitle = "CryptoManager.Net API Documentation";
        c.DefaultModelsExpandDepth(-1); // Hide models section by default
        c.DisplayRequestDuration();
    });
}

app.UseMiddleware<CachingMiddleware>();

app.UseResponseCaching();

app.UseHttpsRedirection();

app.UseWebSockets(new WebSocketOptions
{
    KeepAliveInterval = TimeSpan.FromSeconds(30),
    KeepAliveTimeout = TimeSpan.FromSeconds(10)
});

app.MapControllers();

app.Run();