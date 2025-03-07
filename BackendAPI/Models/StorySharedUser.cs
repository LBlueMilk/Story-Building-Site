using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace BackendAPI.Models
{
    public class StorySharedUser
    {
        [Key]
        public int Id { get; set; } // 共享記錄 ID（主鍵）

        [Required]
        public int StoryId { get; set; } // 共享的故事 ID

        [Required]
        public int UserId { get; set; } // 被授權的使用者 ID

        public DateTime SharedAt { get; set; } = DateTime.UtcNow; // 共享時間

        // 導覽屬性
        [ForeignKey("StoryId")]
        public Story Story { get; set; }

        [ForeignKey("UserId")]
        public User User { get; set; }
    }
}
