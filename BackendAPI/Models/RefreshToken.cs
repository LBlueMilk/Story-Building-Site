using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace BackendAPI.Models
{
    [Table("refresh_tokens")]
    public class RefreshToken
    {
        [Key]
        [Column("id")]
        public int Id { get; set; } // 主鍵，自動增長

        [Required]
        [Column("user_id")]
        public int UserId { get; set; } // 使用者 ID
        public User User { get; set; }

        [Required]
        [Column("token_hash")]
        public string TokenHash { get; set; } // Token 雜湊

        [Column("device_info")]
        public string? DeviceInfo { get; set; } // 裝置資訊

        [Required]
        [Column("expires_at")]
        public DateTime ExpiresAt { get; set; } // 過期時間

        [Column("revoked_at")]
        public DateTime? RevokedAt { get; set; } // 撤銷時間

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow; // 創建時間
    }
}
