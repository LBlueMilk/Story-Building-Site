namespace BackendAPI.Services.User
{
    public interface IUserService
    {
        Task<bool> ChangePasswordAsync(int userId, string newPassword);
        Task<string?> GetUserProviderAsync(int userId);

        // 檢查指定 userId 是否對 storyId 有存取權限
        Task<bool> HasAccessToStoryAsync(int userId, int storyId);
    }
}
