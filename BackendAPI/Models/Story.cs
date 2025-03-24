using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace BackendAPI.Models
{
    [Table("stories")]
    public class Story
    {
        [Key]
        [Column("id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)] // 明確告訴 EF Core 這是自增欄位
        public int Id { get; set; } // 故事 ID（主鍵）

        [Required]
        [Column("user_id")]
        public int CreatorId { get; set; } // 創建者的使用者 ID

        [Required, MaxLength(255)]
        [Column("title")]
        public string Title { get; set; } // 故事標題

        [Column("description")]
        public string? Description { get; set; } // 故事描述 (可為 NULL)

        [Column("is_public")]
        public bool IsPublic { get; set; } = false; // 是否公開

        [Column("public_id")]
        public Guid PublicId { get; set; } // 用於公開分享的 ID（UUID）

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow; // 創建時間

        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; } // 最後更新時間

        [Column("deleted_at")]
        public DateTime? DeletedAt { get; set; } // 軟刪除時間

        [Column("restored_at")]
        public DateTime? RestoredAt { get; set; } // 還原時間

        [Column("share_token")]
        [MaxLength(255)]
        public string? ShareToken { get; set; }  // 分享用的 Token，可為 NULL

        [Column("share_token_expires_at")]
        public DateTime? ShareTokenExpiresAt { get; set; } // Token 過期時間


        // 導覽屬性
        [ForeignKey("CreatorId")]
        [JsonIgnore]
        public virtual User? Creator { get; set; } // 關聯到 `User`，這是 `HasOne(s => s.Creator)` 需要的屬性

        // 該故事被分享的使用者
        public ICollection<StorySharedUser> SharedUsers { get; set; } = new List<StorySharedUser>();
    }
}
