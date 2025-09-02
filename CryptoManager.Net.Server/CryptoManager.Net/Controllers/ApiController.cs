using CryptoManager.Net.Database;
using CryptoManager.Net.Database.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CryptoManager.Net.Controllers
{
    [ApiController]
    public class ApiController : ControllerBase
    {
        protected readonly TrackerContext _dbContext;

        protected int UserId => (int)HttpContext.Items["userId"]!;

        protected ApiController(TrackerContext dbContext)
        {
            _dbContext = dbContext;
        }

        protected async Task<bool> CheckUserUpdateTopicAsync(UserUpdateType type, string? symbol = null)
        {
            var lastUpdate = await _dbContext.UserUpdates.SingleOrDefaultAsync(x => x.UserId == UserId && x.Type == type);
            if (lastUpdate != null && DateTime.UtcNow - lastUpdate.LastUpdate < TimeSpan.FromSeconds(2))
                return false;

            if (lastUpdate == null)
                _dbContext.UserUpdates.Add(new UserUpdate { UserId = UserId, LastUpdate = DateTime.UtcNow, Type = type });
            else
                lastUpdate.LastUpdate = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync();
            return true;
        }
    }
}
