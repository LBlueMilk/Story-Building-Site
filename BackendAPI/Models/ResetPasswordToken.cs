using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace BackendAPI.Models
{
    [Table("reset_password_tokens")]
    public class ResetPasswordToken
    {
        [Key]
        [Column("id")]
        public int Id { get; set; } // 主鍵，自動增長

        [Required]
        [Column("user_id")]
        public int UserId { get; set; } // 使用者 ID

        [Required]
        [Column("token")]
        public string Token { get; set; } = string.Empty; // Token 字串

        [Required]
        [Column("expires_at")]
        public DateTime ExpiresAt { get; set; } // 過期時間

        // 外鍵關聯
        public User User { get; set; } = null!; 
    }
}
