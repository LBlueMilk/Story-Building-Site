namespace BackendAPI.Application.DTOs
{
    public class CreateStoryDto
    {
        // 新增故事的資料傳輸物件
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsPublic { get; set; } = false;
    }
}
