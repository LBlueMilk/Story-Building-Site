using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BackendAPI.Models
{
    [Table("users")] // 指定對應的資料表名稱
    public class User
    {
        [Key]
        [Column("id")]
        public int Id { get; set; } // 主鍵，自動增長

        [Column("email")]
        [StringLength(255)]
        public string? Email { get; set; } // 允許 NULL，因為 OAuth 用戶可能沒有 Email

        [Column("password")]
        public string? PasswordHash { get; set; } // 允許 NULL，因為 OAuth 用戶不需要密碼

        [Column("name")]
        public string Name { get; set; } = string.Empty; // 使用者名稱，預設空字串

        [Column("is_verified")]
        public bool IsVerified { get; set; } = false; // 信箱驗證狀態，預設 false

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow; // 註冊時間，預設當前時間

        [Column("deleted_at")]
        public DateTime? DeletedAt { get; set; }  // 軟刪除時間

        [Column("restored_at")]
        public DateTime? RestoredAt { get; set; } // 還原時間

        // 定義關聯關係
        public ICollection<UserProvider> UserProviders { get; set; } = new List<UserProvider>();

    }
}
