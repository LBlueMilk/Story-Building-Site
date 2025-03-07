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
        // 故事資料表
        public DbSet<Story> Stories { get; set; }
        // 共享故事資料表
        public DbSet<StorySharedUser> StorySharedUsers { get; set; }



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

            // 新增故事的關聯設定
            modelBuilder.Entity<Story>()
                .HasOne(s => s.Creator) // 一個故事只有一個創建者
                .WithMany(u => u.Stories) // 一個使用者可以創建多個故事
                .HasForeignKey(s => s.CreatorId) // `story.creator_id` 關聯到 `users.id`
                .OnDelete(DeleteBehavior.Cascade); // 刪除使用者時，該使用者創建的故事也刪除

            // 設定共享故事的關聯
            modelBuilder.Entity<StorySharedUser>()
                .HasKey(ssu => new { ssu.StoryId, ssu.UserId }); // 設定組合鍵，避免重複授權

            modelBuilder.Entity<StorySharedUser>()
                .HasOne(ssu => ssu.Story) // `StorySharedUser` 連到 `Story`
                .WithMany(s => s.SharedUsers) // 一個故事可以被多個人共享
                .HasForeignKey(ssu => ssu.StoryId)
                .OnDelete(DeleteBehavior.Cascade); // 刪除故事時，該故事的共享記錄也刪除

            modelBuilder.Entity<StorySharedUser>()
                .HasOne(ssu => ssu.User) // `StorySharedUser` 連到 `User`
                .WithMany(u => u.SharedStories) // 一個使用者可以獲得多個共享故事
                .HasForeignKey(ssu => ssu.UserId)
                .OnDelete(DeleteBehavior.Cascade); // 刪除使用者時，該使用者的共享記錄也刪除
        }
    }
}
