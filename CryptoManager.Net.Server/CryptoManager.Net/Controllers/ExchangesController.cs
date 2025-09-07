using CryptoClients.Net;
using CryptoClients.Net.Enums;
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

[ApiController]
[Route("[controller]")]
[AllowAnonymous]
[ResponseCache(Duration = 10, VaryByQueryKeys = ["*"])]
public class ExchangesController : ApiController
{
    private readonly ILogger _logger;
    private readonly string[]? _enabledExchanges;

    public ExchangesController(ILogger<ExchangesController> logger, IConfiguration configuration, MongoTrackerContext dbContext) : base(dbContext)
    {
        _logger = logger;
        _enabledExchanges = configuration.GetValue<string?>("EnabledExchanges")?.Split(";");
    }

    [HttpGet("names")]
    [ResponseCache(Duration = 600)]
    [ServerCache(Duration = 600)]
    public ApiResult<string[]> GetExchangeNamesAsync()
    {
        return ApiResult<string[]>.Ok(_enabledExchanges ?? Exchange.All);
    }

    [HttpGet("{exchange}/environments")]
    [ResponseCache(Duration = 600, VaryByQueryKeys = ["*"])]
    [ServerCache(Duration = 600)]
    public ApiResult<string[]> GetExchangeEnvironmentsAsync(string exchange)
    {
        var exchangeInfo = Exchanges.All.Single(x => x.Name == exchange);

        return ApiResult<string[]>.Ok(exchangeInfo.ApiEnvironments);
    }

    [HttpGet]
    [ServerCache(Duration = 10)]
    public async Task<ApiResultPaged<IEnumerable<ApiExchange>>> GetExchangesAsync(
        string? query = null,
        string? orderBy = null,
        OrderDirection? orderDirection = null,
        int page = 1,
        int pageSize = 20)
    {
        // Build MongoDB aggregation pipeline
        var pipeline = new List<BsonDocument>();

        // Match stage - filter by enabled exchanges
        if (_enabledExchanges != null)
        {
            pipeline.Add(new BsonDocument("$match", new BsonDocument("Exchange", new BsonDocument("$in", new BsonArray(_enabledExchanges)))));
        }

        // Group by Exchange
        var groupStage = new BsonDocument("$group", new BsonDocument
        {
            { "_id", "$Exchange" },
            { "Exchange", new BsonDocument("$first", "$Exchange") },
            { "Symbols", new BsonDocument("$sum", new BsonDocument("$cond", new BsonArray { "$Enabled", 1, 0 })) },
            { "UsdVolume", new BsonDocument("$sum", new BsonDocument("$ifNull", new BsonArray { "$UsdVolume", 0 })) }
        });
        pipeline.Add(groupStage);

        // Filter by query if provided
        if (!string.IsNullOrEmpty(query))
        {
            pipeline.Add(new BsonDocument("$match", new BsonDocument("Exchange", new BsonDocument("$regex", new BsonRegularExpression(query, "i")))));
        }

        // Sort stage
        if (string.IsNullOrEmpty(orderBy))
            orderBy = nameof(ExchangeSymbol.UsdVolume);

        var sortDirection = orderDirection == OrderDirection.Ascending ? 1 : -1;
        var sortField = orderBy switch
        {
            nameof(ApiExchange.UsdVolume) => "UsdVolume",
            nameof(ApiExchange.Symbols) => "Symbols",
            _ => "UsdVolume"
        };
        pipeline.Add(new BsonDocument("$sort", new BsonDocument(sortField, sortDirection)));

        // Count total documents
        var countPipeline = pipeline.ToList();
        countPipeline.Add(new BsonDocument("$count", "total"));
        var totalResult = await _dbContext.Symbols.Aggregate<BsonDocument>(countPipeline).FirstOrDefaultAsync();
        var total = totalResult?["total"]?.AsInt32 ?? 0;

        // Add pagination
        pipeline.Add(new BsonDocument("$skip", (page - 1) * pageSize));
        pipeline.Add(new BsonDocument("$limit", pageSize));

        // Execute aggregation
        var result = await _dbContext.Symbols.Aggregate<BsonDocument>(pipeline).ToListAsync();

        var exchanges = result.Select(x => new ApiExchange
        {
            Exchange = x["Exchange"].AsString,
            UsdVolume = x["UsdVolume"].AsDecimal,
            Symbols = x["Symbols"].AsInt32
        });

        return ApiResultPaged<IEnumerable<ApiExchange>>.Ok(page, pageSize, total, exchanges);
    }

    [HttpGet("{exchange}")]
    [ServerCache(Duration = 10)]
    public async Task<ApiResult<ApiExchangeDetails>> GetExchangeDetailsAsync(string exchange)
    {
        // Build MongoDB aggregation pipeline for specific exchange
        var pipeline = new List<BsonDocument>
        {
            new BsonDocument("$match", new BsonDocument("Exchange", exchange)),
            new BsonDocument("$group", new BsonDocument
            {
                { "_id", "$Exchange" },
                { "Exchange", new BsonDocument("$first", "$Exchange") },
                { "Symbols", new BsonDocument("$sum", new BsonDocument("$cond", new BsonArray { "$Enabled", 1, 0 })) },
                { "UsdVolume", new BsonDocument("$sum", new BsonDocument("$ifNull", new BsonArray { "$UsdVolume", 0 })) }
            })
        };

        var result = await _dbContext.Symbols.Aggregate<BsonDocument>(pipeline).FirstOrDefaultAsync();

        if (result == null)
            return ApiResult<ApiExchangeDetails>.Error(ErrorType.Unknown, null, "Exchange not found");

        var exchangeInfo = Exchanges.All.Single(x => x.Name == exchange);
        return ApiResult<ApiExchangeDetails>.Ok(new ApiExchangeDetails
        {
            Exchange = exchange,
            Symbols = result["Symbols"].AsInt32,
            UsdVolume = result["UsdVolume"].AsDecimal,
            LogoUrl = exchangeInfo.ImageUrl,
            Type = exchangeInfo.Type,
            Url = exchangeInfo.Url
        });
    }

    private class ExchangeProjection
    {
        public string Exchange { get; set; } = string.Empty;
        public decimal UsdVolume { get; set; }
        public int Symbols { get; set; }
    }
}
