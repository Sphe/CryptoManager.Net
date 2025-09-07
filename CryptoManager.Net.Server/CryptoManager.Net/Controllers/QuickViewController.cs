﻿using CryptoExchange.Net.Objects.Errors;
using CryptoManager.Net.Database;
using CryptoManager.Net.Database.Models;
using CryptoManager.Net.Models.Response;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

namespace CryptoManager.Net.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class QuickViewController : ApiController
    {
        private readonly ILogger _logger;
        private int _maxSymbols;

        public QuickViewController(ILogger<QuickViewController> logger, IConfiguration config, MongoTrackerContext dbContext) : base(dbContext)
        {
            _logger = logger;
            _maxSymbols = config.GetValue<int?>("MaxQuickViewSymbols") ?? 8;
        }

        [HttpGet]
        public async Task<ApiResult<IEnumerable<string>>> ListAsync()
        {
            var filter = Builders<UserQuickViewConfiguration>.Filter.Eq(x => x.UserId, UserId);
            var result = await _dbContext.UserQuickViewConfigurations.Find(filter).ToListAsync();
            return ApiResult<IEnumerable<string>>.Ok(result.Select(x => x.SymbolId));
        }

        [HttpPost]
        public async Task<ApiResult> AddSymbolAsync([FromBody]ApiQuickViewConfig request)
        {
            var filter = Builders<UserQuickViewConfiguration>.Filter.Eq(x => x.UserId, UserId);
            var currentCount = await _dbContext.UserQuickViewConfigurations.CountDocumentsAsync(filter);
            if (currentCount >= _maxSymbols)
                return ApiResult.Error(ErrorType.Unknown, null, "Too many symbols");

            var newConfig = new UserQuickViewConfiguration
            {
                Id = Guid.NewGuid().ToString(),
                SymbolId = request.Symbol,
                UserId = UserId
            };

            await _dbContext.UserQuickViewConfigurations.InsertOneAsync(newConfig);

            return ApiResult.Ok();
        }


        [HttpDelete("{symbolId}")]
        public async Task<ApiResult> RemoveSymbolAsync(string symbolId)
        {
            var filter = Builders<UserQuickViewConfiguration>.Filter.And(
                Builders<UserQuickViewConfiguration>.Filter.Eq(x => x.UserId, UserId),
                Builders<UserQuickViewConfiguration>.Filter.Eq(x => x.SymbolId, symbolId)
            );
            
            var result = await _dbContext.UserQuickViewConfigurations.DeleteOneAsync(filter);
            
            return ApiResult.Ok();
        }
    }
}
