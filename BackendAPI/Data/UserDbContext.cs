using BackendAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace BackendAPI.Data
{

    // `UserDbContext` 繼承 `DbContext`，負責與資料庫互動
    public class UserDbContext : DbContext
    {
        // `DbContextOptions` 讓 `Program.cs` 設定資料庫連線
        public UserDbContext(DbContextOptions<UserDbContext> options) : base(options) { }

        // 這個 `DbSet<User>` 代表對應 `users` 資料表
        public DbSet<User> Users { get; set; }

        // 設定資料庫 Schema（可選）
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email) // 設定 Email 為唯一
                .IsUnique();

            modelBuilder.Entity<User>()
                .Property(u => u.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP"); // 設定 CreatedAt 預設值為當前時間
        }
    }

}
