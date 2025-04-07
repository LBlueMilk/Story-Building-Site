using BackendAPI.Application.DTOs;

namespace BackendAPI.Services.Storage
{
    public interface IStorageService
    {
        // 定義獲取和保存用戶故事的方法
        Task<string?> GetCanvasJsonAsync(int storyId, int userId);
        Task SaveCanvasJsonAsync(int storyId, int userId, string json, DateTime lastModified);
        Task<JsonWithModifiedDto?> GetCanvasWithLastModifiedAsync(int storyId, int userId);

        // 定義獲取和保存用戶角色的方法
        Task<string?> GetCharacterJsonAsync(int storyId, int userId);
        Task SaveCharacterJsonAsync(int storyId, int userId, string json, DateTime lastModified);
        Task<JsonWithModifiedDto?> GetCharacterWithLastModifiedAsync(int storyId, int userId);

        // 定義獲取和保存用戶時間線的方法
        Task<string?> GetTimelineJsonAsync(int storyId, int userId);
        Task SaveTimelineJsonAsync(int storyId, int userId, string json, DateTime lastModified);            
        Task<JsonWithModifiedDto?> GetTimelineWithLastModifiedAsync(int storyId, int userId);

    }
}
