using BackendAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace BackendAPI.Data
{
    public class UserDbContext : DbContext
    {
        public UserDbContext(DbContextOptions<UserDbContext> options) : base(options) { }

        // 定義資料表與對應的 Model 類別
        public DbSet<User> Users { get; set; }        
        public DbSet<UserProvider> UserProviders { get; set; }        
        public DbSet<Story> Stories { get; set; }        
        public DbSet<StorySharedUser> StorySharedUsers { get; set; }        
        public DbSet<RefreshToken> RefreshTokens { get; set; }
        public DbSet<ResetPasswordToken> ResetPasswordTokens { get; set; }



        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            modelBuilder.Entity<User>()
                .Property(u => u.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
            
            modelBuilder.Entity<UserProvider>()
                .HasIndex(up => new { up.UserId, up.Provider })
                .IsUnique();

            modelBuilder.Entity<UserProvider>()
                .Property(up => up.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            modelBuilder.Entity<UserProvider>()
                .HasOne(up => up.User)
                .WithMany(u => u.UserProviders)
                .HasForeignKey(up => up.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            
            modelBuilder.Entity<Story>()
                .HasOne(s => s.Creator)
                .WithMany(u => u.Stories)
                .HasForeignKey(s => s.CreatorId)
                .OnDelete(DeleteBehavior.Cascade);
            
            modelBuilder.Entity<StorySharedUser>()
                .HasKey(ssu => new { ssu.StoryId, ssu.UserId });

            modelBuilder.Entity<StorySharedUser>()
                .HasOne(ssu => ssu.Story)
                .WithMany(s => s.SharedUsers)
                .HasForeignKey(ssu => ssu.StoryId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<StorySharedUser>()
                .HasOne(ssu => ssu.User)
                .WithMany(u => u.SharedStories)
                .HasForeignKey(ssu => ssu.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            
            modelBuilder.Entity<RefreshToken>()
                .HasOne(rt => rt.User)
                .WithMany(u => u.RefreshTokens)
                .HasForeignKey(rt => rt.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            
            modelBuilder.Entity<RefreshToken>()
                .HasIndex(rt => rt.TokenHash)
                .IsUnique();

            modelBuilder.Entity<RefreshToken>()
                .ToTable(t => t.HasCheckConstraint("CK_RefreshToken_ExpiresAt", "expires_at > CURRENT_TIMESTAMP"));

            modelBuilder.Entity<RefreshToken>()
                .HasIndex(rt => new { rt.UserId, rt.TokenHash })
                .IsUnique();

            modelBuilder.Entity<ResetPasswordToken>()
               .HasOne(rt => rt.User)
               .WithMany()
               .HasForeignKey(rt => rt.UserId)
               .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ResetPasswordToken>()
               .HasIndex(rt => new { rt.UserId, rt.Token })
               .IsUnique();
            
            modelBuilder.Entity<ResetPasswordToken>()
                .ToTable(t => t.HasCheckConstraint("CK_ResetPasswordToken_ExpiresAt", "expires_at > CURRENT_TIMESTAMP"));
        }
    }
}
