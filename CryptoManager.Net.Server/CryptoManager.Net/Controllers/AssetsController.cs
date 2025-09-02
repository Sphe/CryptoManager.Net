using CryptoManager.Net.Caching;
using CryptoManager.Net.Database;
using CryptoManager.Net.Database.Models;
using CryptoManager.Net.Models.Requests;
using CryptoManager.Net.Models.Response;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
        TrackerContext dbContext) : base(dbContext)
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
        IQueryable<AssetStats> preGroupQuery = _dbContext.AssetStats;
        if (_enabledExchanges != null)
            preGroupQuery = preGroupQuery.Where(x => _enabledExchanges.Contains(x.Exchange));

        if (!string.IsNullOrEmpty(exchange))
            preGroupQuery = preGroupQuery.Where(x => x.Exchange == exchange);

        if (assetType != null)
            preGroupQuery = preGroupQuery.Where(x => x.AssetType == assetType.Value);

        var dbQuery = preGroupQuery.GroupBy(x => x.Asset);
        if (!string.IsNullOrEmpty(query))
            dbQuery = dbQuery.Where(x => x.Key.Contains(query));

        if (string.IsNullOrEmpty(orderBy))
            orderBy = nameof(ApiAsset.VolumeUsd);

        Expression<Func<IGrouping<string, AssetStats>, decimal?>> order = orderBy switch
        {
            nameof(ApiAsset.Volume) => asset => asset.Sum(x => x.Volume),
            nameof(ApiAsset.VolumeUsd) => asset => asset.Sum(x => x.Volume * x.Value),
            nameof(ApiAsset.ChangePercentage) => asset => asset.Average(x => x.ChangePercentage),
            _ => throw new ArgumentException(),
        };
        
        dbQuery = orderDirection == OrderDirection.Ascending
            ? dbQuery.OrderBy(order)
            : dbQuery.OrderByDescending(order);

        var selectQuery = dbQuery.Select(x => new
        {
            Name = x.Key,
            AssetType = x.First().AssetType,
            Value = x.Average(x => x.Value),
            Volume = x.Sum(x => x.Volume),
            VolumeUsd = x.Sum(x => x.Volume * x.Value),
            ChangePercentage = x.Average(x => x.ChangePercentage),
        }).Where(x => x.VolumeUsd > minUsdVolume);

        var total = await selectQuery.CountAsync();
        var result = await selectQuery.Skip((page - 1) * pageSize).Take(pageSize).AsNoTracking().ToListAsync();

        var pageResult = ApiResultPaged<IEnumerable<ApiAsset>>.Ok(page, pageSize, total, result.Select(x => new ApiAsset
        {
            Name = x.Name,
            AssetType = x.AssetType,
            Value = x.Value,
            Volume = x.Volume,
            VolumeUsd = x.Value * x.Volume,
            ChangePercentage = x.ChangePercentage
        }));

        return pageResult;
    }
}
