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
public class AssetsController : ApiController
{
    private readonly ILogger _logger;
    private readonly string[]? _enabledExchanges;

    public AssetsController(
        ILogger<AssetsController> logger, 
        IConfiguration configuration,
        MongoTrackerContext dbContext) : base(dbContext)
    {
        _logger = logger;
        _enabledExchanges = configuration.GetValue<string?>("EnabledExchanges")?.Split(";");
    }

    [HttpGet]
    [ResponseCache(Duration = 10, VaryByQueryKeys = ["*"])]
    [ServerCache(Duration = 10)]
    public async Task<ApiResultPaged<IEnumerable<ApiAsset>>> GetAssetsAsync(
        string? query = null,
        string? orderBy = null,
        string? exchange = null,
        OrderDirection? orderDirection = null,
        AssetType? assetType = null,
        int minUsdVolume = 0,
        int page = 1,
        int pageSize = 20)
    {
        // Build MongoDB aggregation pipeline
        var pipeline = new List<BsonDocument>();

        // Match stage - filter by enabled exchanges, exchange, and asset type
        var matchFilters = new List<BsonElement>();
        
        if (_enabledExchanges != null)
            matchFilters.Add(new BsonElement("Exchange", new BsonDocument("$in", new BsonArray(_enabledExchanges))));
        
        if (!string.IsNullOrEmpty(exchange))
            matchFilters.Add(new BsonElement("Exchange", exchange));
        
        if (assetType != null)
            matchFilters.Add(new BsonElement("AssetType", (int)assetType.Value));

        if (matchFilters.Any())
            pipeline.Add(new BsonDocument("$match", new BsonDocument(matchFilters)));

        // Group by Asset
        var groupStage = new BsonDocument("$group", new BsonDocument
        {
            { "_id", "$Asset" },
            { "Name", new BsonDocument("$first", "$Asset") },
            { "AssetType", new BsonDocument("$first", "$AssetType") },
            { "Value", new BsonDocument("$avg", "$Value") },
            { "Volume", new BsonDocument("$sum", "$Volume") },
            { "VolumeUsd", new BsonDocument("$sum", new BsonDocument("$multiply", new BsonArray { "$Volume", "$Value" })) },
            { "ChangePercentage", new BsonDocument("$avg", "$ChangePercentage") }
        });
        pipeline.Add(groupStage);

        // Filter by query if provided
        if (!string.IsNullOrEmpty(query))
            pipeline.Add(new BsonDocument("$match", new BsonDocument("Name", new BsonDocument("$regex", new BsonRegularExpression(query, "i")))));

        // Filter by minimum USD volume
        if (minUsdVolume > 0)
            pipeline.Add(new BsonDocument("$match", new BsonDocument("VolumeUsd", new BsonDocument("$gt", minUsdVolume))));

        // Sort stage
        if (string.IsNullOrEmpty(orderBy))
            orderBy = nameof(ApiAsset.VolumeUsd);

        var sortDirection = orderDirection == OrderDirection.Ascending ? 1 : -1;
        var sortField = orderBy switch
        {
            nameof(ApiAsset.Volume) => "Volume",
            nameof(ApiAsset.VolumeUsd) => "VolumeUsd", 
            nameof(ApiAsset.ChangePercentage) => "ChangePercentage",
            _ => "VolumeUsd"
        };
        pipeline.Add(new BsonDocument("$sort", new BsonDocument(sortField, sortDirection)));

        // Count total documents
        var countPipeline = pipeline.ToList();
        countPipeline.Add(new BsonDocument("$count", "total"));
        var totalResult = await _dbContext.AssetStats.Aggregate<BsonDocument>(countPipeline).FirstOrDefaultAsync();
        var total = totalResult?["total"]?.AsInt32 ?? 0;

        // Add pagination
        pipeline.Add(new BsonDocument("$skip", (page - 1) * pageSize));
        pipeline.Add(new BsonDocument("$limit", pageSize));

        // Execute aggregation
        var result = await _dbContext.AssetStats.Aggregate<BsonDocument>(pipeline).ToListAsync();

        var assets = result.Select(x => new ApiAsset
        {
            Name = x["Name"].AsString,
            AssetType = (AssetType)x["AssetType"].AsInt32,
            Value = (decimal?)x["Value"].AsDecimal,
            Volume = x["Volume"].AsDecimal,
            VolumeUsd = x["VolumeUsd"].AsDecimal,
            ChangePercentage = (decimal?)x["ChangePercentage"].AsDecimal
        });

        return ApiResultPaged<IEnumerable<ApiAsset>>.Ok(page, pageSize, total, assets);
    }
}
