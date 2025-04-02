namespace BackendAPI.Services
{
    public interface IStoryDataService
    {
        // 定義獲取和保存故事數據的方法
        Task<string?> GetCanvasJsonAsync(int storyId);
        Task SaveCanvasJsonAsync(int storyId, string json);

        // 定義獲取和保存角色數據的方法
        Task<string?> GetCharacterJsonAsync(int storyId);
        Task SaveCharacterJsonAsync(int storyId, string json);

        // 定義獲取和保存時間線數據的方法
        Task<string?> GetTimelineJsonAsync(int storyId);
        Task SaveTimelineJsonAsync(int storyId, string json);
    }
}
