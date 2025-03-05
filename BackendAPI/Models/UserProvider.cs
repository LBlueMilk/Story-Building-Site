using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BackendAPI.Models
{
    [Table("user_providers")] // 對應資料庫中的表
    public class UserProvider
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [Column("user_id")]
        public int UserId { get; set; }

        [Column("provider")]
        public string Provider { get; set; } = string.Empty;

        [Column("provider_id")]
        public string ProviderId { get; set; } = string.Empty;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow; // 註冊時間，預設當前時間

        // 外鍵關聯
        public User User { get; set; }
    }
}
