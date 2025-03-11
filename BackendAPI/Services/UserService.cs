using BackendAPI.Data;

namespace BackendAPI.Services
{
    public class UserService : IUserService
    {
        private readonly UserDbContext _context;

        public UserService(UserDbContext context)
        {
            _context = context;
        }

        public async Task<bool> ChangePasswordAsync(int userId, string newPassword)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return false;

            // 1️⃣ 更新密碼
            user.PasswordHash = HashPassword(newPassword);
            user.LastPasswordChange = DateTime.UtcNow;

            // 2️⃣ 刪除所有舊的 Refresh Token
            _context.RefreshTokens.RemoveRange(
                _context.RefreshTokens.Where(rt => rt.UserId == userId)
            );

            // 3️⃣ 存檔
            await _context.SaveChangesAsync();
            return true;
        }

        private string HashPassword(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password);
        }
    }

}
