using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BackendAPI.Models
{
    [Table("users")] // 指定對應的資料表名稱
    [Index(nameof(Email), IsUnique = true)] // 確保 Email 唯一性，提升查詢效率
    [Index(nameof(DeletedAt))] // 軟刪除查詢時提升效能
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
        [StringLength(100)]
        public string? Name { get; set; } // 使用者名稱，預設空字串

        [Column("is_verified")]
        public bool IsVerified { get; set; } = false; // 信箱驗證狀態，預設 false

        [Column("email_verification_token")]
        [StringLength(36)]
        public string? EmailVerificationToken { get; set; } // Email 驗證 Token

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow; // 註冊時間，預設當前時間

        [Column("deleted_at")]
        public DateTime? DeletedAt
        {
            get => _deletedAt;
            set => _deletedAt = value?.ToUniversalTime(); //  確保轉換為 UTC
        }
        private DateTime? _deletedAt;  // 軟刪除時間

        [Column("restored_at")]
        public DateTime? RestoredAt
        {
            get => _restoredAt;
            set => _restoredAt = value?.ToUniversalTime(); //  確保轉換為 UTC
        }
        private DateTime? _restoredAt; // 還原時間

        [Column("last_login")]
        public DateTime? LastLogin
        {
            get => _lastLogin;
            set => _lastLogin = value?.ToUniversalTime(); // 確保轉換為 UTC
        }
        private DateTime? _lastLogin; // 最後成功登入時間

        [Column("failed_login_attempts")]
        public int FailedLoginAttempts { get; set; } = 0; // 失敗登入次數

        [Column("last_failed_login")]
        public DateTime? LastFailedLogin
        {
            get => _lastFailedLogin;
            set => _lastFailedLogin = value?.ToUniversalTime();
        }
        private DateTime? _lastFailedLogin; // 最後一次登入失敗時間

        [Column("last_password_change")]
        public DateTime? LastPasswordChange
        {
            get => _lastPasswordChange;
            set => _lastPasswordChange = value?.ToUniversalTime();
        }
        private DateTime? _lastPasswordChange; // 記錄最後密碼變更時間

        // 使用者代碼，用於分享故事
        [Required]
        [Column("user_code")]
        [MaxLength(8)]
        public string UserCode { get; set; } = string.Empty;



        // 定義關聯關係
        public virtual ICollection<UserProvider> UserProviders { get; set; } = new List<UserProvider>();
        public virtual ICollection<Story> Stories { get; set; } = new List<Story>(); // 該使用者創建的故事
        public virtual ICollection<StorySharedUser> SharedStories { get; set; } = new List<StorySharedUser>(); // 該使用者被分享的故事
        public virtual ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>(); // 該使用者的 Refresh Token

    }
}
