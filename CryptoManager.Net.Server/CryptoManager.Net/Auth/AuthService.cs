using CryptoManager.Net.Database;
using CryptoManager.Net.Database.Models;
using Microsoft.EntityFrameworkCore;

namespace CryptoManager.Net.Auth
{
    public class AuthService
    {
        private readonly TrackerContext _context;
        private readonly JwtService _jwtService;

        public AuthService(TrackerContext context, JwtService jwtService)
        {
            _jwtService = jwtService;
            _context = context;
        }

        public async Task<RefreshToken> CreateRefreshTokenAsync(int userId)
        {
            var expired = await _context.RefreshTokens.Where(x => x.UserId == userId && x.ExpireTime < DateTime.UtcNow).ToListAsync();

            var token = _jwtService.CreateRefreshToken(userId);
            _context.RefreshTokens.Add(token);
            if (expired.Any())
                _context.RefreshTokens.RemoveRange(expired);
            await _context.SaveChangesAsync();
            return token;
        }

        public async Task<RefreshToken> RefreshRefreshTokenAsync(int tokenId)
        {
            var dbToken = await _context.RefreshTokens.SingleAsync(x => x.Id == tokenId);
            _jwtService.RefreshRefreshToken(dbToken);
            await _context.SaveChangesAsync();
            return dbToken;
        }
    }
}
