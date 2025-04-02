using BackendAPI.Data;
using Microsoft.EntityFrameworkCore;

namespace BackendAPI.Services.User
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

            // 更新密碼
            user.PasswordHash = HashPassword(newPassword);
            user.LastPasswordChange = DateTime.UtcNow;

            // 刪除所有舊的 Refresh Token
            _context.RefreshTokens.RemoveRange(
                _context.RefreshTokens.Where(rt => rt.UserId == userId)
            );

            // 存檔
            await _context.SaveChangesAsync();
            return true;
        }

        private string HashPassword(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password);
        }

        // 檢查是否為 Google 登入的使用者
        public async Task<bool> IsGoogleUserAsync(int userId)
        {
            return await _context.UserProviders
                .AnyAsync(up => up.UserId == userId && up.Provider == "Google");
        }

        // 取得使用者的登入提供者
        public async Task<string?> GetUserProviderAsync(int userId)
        {
            return await _context.UserProviders
                .Where(x => x.UserId == userId)
                .Select(x => x.Provider)
                .FirstOrDefaultAsync();
        }

        // 判斷是否為該故事的擁有者（可根據需求擴充為分享權限判斷）
        public async Task<bool> HasAccessToStoryAsync(int userId, int storyId)
        {
            return await _context.Stories.AnyAsync(s => s.Id == storyId && s.CreatorId == userId);
        }
    }

}
