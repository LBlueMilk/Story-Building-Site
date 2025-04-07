using BackendAPI.Models;

namespace BackendAPI.Services.User
{
    public interface IUserService
    {
        Task<bool> ChangePasswordAsync(int userId, string newPassword);
        Task<string?> GetUserProviderAsync(int userId);

        // 檢查指定 userId 是否對 storyId 有存取權限
        Task<bool> HasAccessToStoryAsync(int userId, int storyId);

        // 取得目前登入者的 userId（從 JWT Token 解析）
        int GetUserId();

        // 取得目前登入者的使用者名稱
        Task<List<Story>> GetUserStoriesAsync(int userId);

        // 取得故事ID
        Task<bool> StoryExistsAsync(int storyId);

    }
}
