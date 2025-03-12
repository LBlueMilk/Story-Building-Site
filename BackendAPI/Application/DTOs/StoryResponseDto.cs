namespace BackendAPI.Application.DTOs
{
    public class StoryResponseDto
    {
        public int Id { get; set; }  // 故事的唯一 ID（可選，前端可能不需要）
        public Guid PublicId { get; set; }  // 用於前端識別的安全 UUID
        public string Title { get; set; }  // 故事標題
        public string? Description { get; set; }  // 故事描述（可為 NULL）
        public bool IsPublic { get; set; }  // 是否為公開故事
        public DateTime CreatedAt { get; set; }  // 創建時間
        public DateTime? UpdatedAt { get; set; }  // 最後更新時間（可為 NULL）
        public DateTime? DeletedAt { get; set; }  // 新增：刪除時間
    }

}
