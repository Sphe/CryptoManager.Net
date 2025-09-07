using CryptoClients.Net.Interfaces;
using CryptoExchange.Net.Objects.Errors;
using CryptoManager.Net.Caching;
using CryptoManager.Net.Database;
using CryptoManager.Net.Database.Models;
using CryptoManager.Net.Models.Requests;
using CryptoManager.Net.Models.Response;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using MongoDB.Bson;
using System.Linq.Expressions;

namespace CryptoManager.Net.Controllers;

[Route("[controller]")]
[AllowAnonymous]
[ResponseCache(Duration = 10, VaryByQueryKeys = ["*"])]
public class SymbolsController : ApiController
{
    private readonly ILogger _logger;
    private readonly IExchangeRestClient _restClient;

    public SymbolsController(ILogger<SymbolsController> logger, MongoTrackerContext dbContext, IExchangeRestClient restClient) : base(dbContext)
    {
        _logger = logger;
        _restClient = restClient;
    }

    [HttpGet]
    [ServerCache(Duration = 10)]
    public async Task<ApiResultPaged<IEnumerable<ApiSymbol>>> GetSymbolsAsync(
        string? query = null,
        string? exchange = null,
        string? baseAsset = null,
        string? quoteAsset = null,
        int minUsdVolume = 0,
        string? orderBy = null,
        OrderDirection? orderDirection = null,
        int page = 1,
        int pageSize = 20)
    {
        // Build MongoDB filter
        var filterBuilder = Builders<ExchangeSymbol>.Filter;
        var filters = new List<FilterDefinition<ExchangeSymbol>>();

        filters.Add(filterBuilder.Gte(x => x.UsdVolume, minUsdVolume));

        if (!string.IsNullOrEmpty(exchange))
            filters.Add(filterBuilder.Eq(x => x.Exchange, exchange));

        if (!string.IsNullOrEmpty(query))
            filters.Add(filterBuilder.Regex(x => x.Name, new BsonRegularExpression(query, "i")));

        if (!string.IsNullOrEmpty(baseAsset))
            filters.Add(filterBuilder.Eq(x => x.BaseAsset, baseAsset));

        if (!string.IsNullOrEmpty(quoteAsset))
            filters.Add(filterBuilder.Eq(x => x.QuoteAsset, quoteAsset));

        var filter = filters.Count > 1 ? filterBuilder.And(filters) : filters.FirstOrDefault() ?? filterBuilder.Empty;

        // Build sort
        if (string.IsNullOrEmpty(orderBy))
            orderBy = nameof(ExchangeSymbol.UsdVolume);

        var sortDirection = orderDirection == OrderDirection.Ascending ? SortDirection.Ascending : SortDirection.Descending;
        var sortField = orderBy switch
        {
            nameof(ApiSymbol.Volume) => "Volume",
            nameof(ApiSymbol.QuoteVolume) => "QuoteVolume",
            nameof(ApiSymbol.UsdVolume) => "UsdVolume",
            nameof(ApiSymbol.ChangePercentage) => "ChangePercentage",
            _ => "UsdVolume"
        };

        var sort = Builders<ExchangeSymbol>.Sort.Descending(sortField);
        if (sortDirection == SortDirection.Ascending)
            sort = Builders<ExchangeSymbol>.Sort.Ascending(sortField);

        // Get total count
        var total = await _dbContext.Symbols.CountDocumentsAsync(filter);

        // Get paginated results
        var result = await _dbContext.Symbols
            .Find(filter)
            .Sort(sort)
            .Skip((page - 1) * pageSize)
            .Limit(pageSize)
            .ToListAsync();

        return ApiResultPaged<IEnumerable<ApiSymbol>>.Ok(page, pageSize, (int)total, result.Select(x => new ApiSymbol
        {
            Id = x.Id,
            Name = x.BaseAsset + "/" + x.QuoteAsset,
            Exchange = x.Exchange,
            BaseAsset = x.BaseAsset,
            QuoteAsset = x.QuoteAsset,
            LastPrice = x.LastPrice,
            Volume = x.Volume ?? 0,
            QuoteVolume = x.QuoteVolume,
            UsdVolume = x.UsdVolume,
            ChangePercentage = x.ChangePercentage
        }));
    }

    [HttpGet("{id}")]
    [ServerCache(Duration = 10)]
    public async Task<ApiResult<ApiSymbolDetails>> GetSymbolAsync(string id)
    {
        var filter = Builders<ExchangeSymbol>.Filter.Eq(x => x.Id, id);
        var symbol = await _dbContext.Symbols.Find(filter).FirstOrDefaultAsync();
        if (symbol == null)
        {
            _logger.LogDebug("Symbol with id {Id} not found", id);
            return ApiResult<ApiSymbolDetails>.Error(ErrorType.UnknownSymbol, null, "Symbol not found");
        }

        var client = _restClient.GetSpotOrderClient(symbol.Exchange);
        return ApiResult<ApiSymbolDetails>.Ok(new ApiSymbolDetails
        {
            Id = symbol.Id,
            Exchange = symbol.Exchange,
            BaseAsset = symbol.BaseAsset,
            QuoteAsset = symbol.QuoteAsset,
            ChangePercentage = symbol.ChangePercentage,
            LastPrice = symbol.LastPrice,
            Name = symbol.Name,
            QuoteVolume = symbol.QuoteVolume,
            UsdVolume = symbol.UsdVolume,
            Volume = symbol.Volume ?? 0,

            MinNotionalValue = symbol.MinNotionalValue,
            MinTradeQuantity = symbol.MinTradeQuantity,
            PriceDecimals = symbol.PriceDecimals,
            PriceSignificantFigures = symbol.PriceSignificantFigures,
            PriceStep = symbol.PriceStep,
            QuantityDecimals = symbol.QuantityDecimals,
            QuantityStep = symbol.QuantityStep,
            
            SupportPlacement = client?.PlaceSpotOrderOptions.Supported == true,
            FeeAssetType = client?.SpotFeeAssetType,
            FeeDeductionType = client?.SpotFeeDeductionType,
            SupportedTimeInForces = client?.SpotSupportedTimeInForce,
            SupportedOrderTypes = client?.SpotSupportedOrderTypes,
            SupportedQuantities = client?.SpotSupportedOrderQuantity,
        });
    }

    [HttpGet("names")]
    [ResponseCache(Duration = 600, VaryByQueryKeys = ["*"])]
    [ServerCache(Duration = 600)]
    public async Task<ApiResult<ApiExchangeSymbols>> GetSymbolNamesAsync(
        string exchange,
        string? baseAsset = null,
        string? quoteAsset = null)
    {
        // Build MongoDB aggregation pipeline
        var pipeline = new List<BsonDocument>();

        // Match stage
        var matchFilters = new List<BsonElement>
        {
            new BsonElement("Exchange", exchange)
        };

        if (!string.IsNullOrEmpty(baseAsset))
            matchFilters.Add(new BsonElement("BaseAsset", baseAsset));
        if (!string.IsNullOrEmpty(quoteAsset))
            matchFilters.Add(new BsonElement("QuoteAsset", quoteAsset));

        pipeline.Add(new BsonDocument("$match", new BsonDocument(matchFilters)));

        // Group by BaseAsset and collect QuoteAssets
        pipeline.Add(new BsonDocument("$group", new BsonDocument
        {
            { "_id", "$BaseAsset" },
            { "QuoteAssets", new BsonDocument("$push", "$QuoteAsset") }
        }));

        // Execute aggregation
        var aggregationResult = await _dbContext.Symbols.Aggregate<BsonDocument>(pipeline).ToListAsync();

        // Convert to dictionary
        var result = aggregationResult.ToDictionary(
            x => x["_id"].AsString,
            x => x["QuoteAssets"].AsBsonArray.Select(y => y.AsString).ToList()
        );

        return ApiResult<ApiExchangeSymbols>.Ok(new ApiExchangeSymbols { Exchange = exchange, Symbols = result });
    }
}
