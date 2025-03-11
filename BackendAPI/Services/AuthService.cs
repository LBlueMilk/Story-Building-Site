using BackendAPI.Data;
using Microsoft.EntityFrameworkCore;


namespace BackendAPI.Services
{
    public class AuthService : IAuthService
    {
        private readonly UserDbContext _context;

        public AuthService(UserDbContext context)
        {
            _context = context;
        }

        public async Task<bool> RevokeRefreshTokenAsync(int userId, string refreshToken)
        {
            var tokenHash = HashToken(refreshToken); // 雜湊 Token，避免明文存儲
            var token = await _context.RefreshTokens
                .FirstOrDefaultAsync(rt => rt.UserId == userId && rt.TokenHash == tokenHash);

            if (token == null) return false;

            token.RevokedAt = DateTime.UtcNow; // 標記為撤銷
            await _context.SaveChangesAsync();

            return true;
        }

        private string HashToken(string token)
        {
            return BCrypt.Net.BCrypt.HashPassword(token);
        }
    }

}
