using CryptoManager.Net.Database;
using CryptoManager.Net.Database.Models;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

namespace CryptoManager.Net.Controllers
{
    [ApiController]
    public class ApiController : ControllerBase
    {
        protected readonly MongoTrackerContext _dbContext;

        protected string UserId => HttpContext.Items["userId"]?.ToString() ?? string.Empty;

        protected ApiController(MongoTrackerContext dbContext)
        {
            _dbContext = dbContext;
        }

        protected async Task<bool> CheckUserUpdateTopicAsync(UserUpdateType type, string? symbol = null)
        {
            var lastUpdate = await _dbContext.UserUpdates.Find(x => x.UserId == UserId && x.Type == type).FirstOrDefaultAsync();
            if (lastUpdate != null && DateTime.UtcNow - lastUpdate.LastUpdate < TimeSpan.FromSeconds(2))
                return false;

            if (lastUpdate == null)
            {
                var newUpdate = new UserUpdate { UserId = UserId, LastUpdate = DateTime.UtcNow, Type = type };
                await _dbContext.UserUpdates.InsertOneAsync(newUpdate);
            }
            else
            {
                var update = Builders<UserUpdate>.Update.Set(x => x.LastUpdate, DateTime.UtcNow);
                await _dbContext.UserUpdates.UpdateOneAsync(x => x.Id == lastUpdate.Id, update);
            }
            return true;
        }
    }
}
