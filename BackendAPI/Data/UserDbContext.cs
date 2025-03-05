using BackendAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace BackendAPI.Data
{
    public class UserDbContext : DbContext
    {
        public UserDbContext(DbContextOptions<UserDbContext> options) : base(options) { }

        // `Users` 代表 `users` 資料表，對應 `User` 實體
        public DbSet<User> Users { get; set; }

        // `UserProviders` 代表 `user_providers` 資料表，對應 `UserProvider` 實體
        public DbSet<UserProvider> UserProviders { get; set; }

        // 透過 `OnModelCreating` 方法來設定資料庫 Schema（例如索引、關聯、預設值等）
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // 設定 `Users` 資料表的 Email 欄位為唯一索引，避免重複註冊相同 Email
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            // 設定 `Users` 資料表的 `CreatedAt` 欄位預設值為當前時間
            modelBuilder.Entity<User>()
                .Property(u => u.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            // 設定 `UserProviders` 資料表的 `CreatedAt` 欄位預設值為當前時間
            modelBuilder.Entity<UserProvider>()
                .Property(up => up.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            // 設定 `UserProviders` 的唯一索引，確保相同 `UserId` 不會重複存入相同 `Provider`
            modelBuilder.Entity<UserProvider>()
                .HasIndex(up => new { up.UserId, up.Provider })
                .IsUnique();

            // 設定 `UserProviders` 資料表的外鍵關聯
            modelBuilder.Entity<UserProvider>()
                .HasOne(up => up.User)         // `UserProvider` 參考 `User`
                .WithMany(u => u.UserProviders) // `User` 一個人可以有多個 `UserProvider`
                .HasForeignKey(up => up.UserId) // `UserProvider` 透過 `UserId` 來關聯 `User`
                .OnDelete(DeleteBehavior.Cascade); // 當 `User` 被刪除時，相關 `UserProvider` 也會被自動刪除
        }
    }
}
