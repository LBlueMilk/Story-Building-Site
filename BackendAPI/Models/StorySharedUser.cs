using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace BackendAPI.Models
{
    [Table("story_shared_users")]
    public class StorySharedUser
    {
        [Key]
        [Column("id")]
        public int Id { get; set; } // 共享記錄 ID（主鍵）

        [Required]
        [Column("story_id")]
        public int StoryId { get; set; } // 共享的故事 ID

        [Required]
        [Column("user_id")]
        public int UserId { get; set; } // 被授權的使用者 ID

        [Column("granted_at")]
        public DateTime SharedAt { get; set; } = DateTime.UtcNow; // 共享時間

        // 外鍵StoryId欄位
        [ForeignKey("StoryId")]
        public Story Story { get; set; }

        // 外鍵UserId欄位
        [ForeignKey("UserId")]
        public User User { get; set; }
    }
}
