using BackendAPI.Data;
using BackendAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace BackendAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StoryController : ControllerBase
    {
        private readonly UserDbContext _context;

        public StoryController(UserDbContext context)
        {
            _context = context;
        }

        // 取得使用者的所有故事，包括自己創建的與共享給他的故事
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> GetStories()
        {
            // 取得當前登入的使用者 ID
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);

            // 查詢使用者的所有故事：
            // 1. 自己創建的 (`CreatorId == userId`)
            // 2. 其他人共享給他的 (`SharedUsers` 包含 `userId`)
            var stories = await _context.Stories
                .Where(s => s.CreatorId == userId || s.SharedUsers.Any(su => su.UserId == userId))
                .ToListAsync();

            // 回傳查詢到的故事清單
            return Ok(stories);
        }

        // 新增故事
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> CreateStory([FromBody] Story story)
        {            
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);

            // 移除 Creator 屬性，避免使用者自行設定
            ModelState.Remove("Creator");

            // 直接使用當前登入的使用者 ID
            var newStory = new Story
            {
                CreatorId = userId,
                Title = story.Title,
                Description = story.Description,
                CreatedAt = DateTime.UtcNow,
                PublicId = Guid.NewGuid(),
                IsPublic = story.IsPublic
            };

            _context.Stories.Add(newStory);
            await _context.SaveChangesAsync();

            return Ok(newStory);
        }

        // 更新故事
        [Authorize]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateStory(int id, [FromBody] Story updatedStory)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);

            // 移除 Creator 屬性，避免使用者自行設定
            ModelState.Remove("Creator");

            var story = await _context.Stories
                .Include(s => s.SharedUsers)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (story == null)
                return NotFound(new { message = "找不到故事" });

            // 只有創建者或共享編輯者可以修改內容
            bool isOwner = story.CreatorId == userId;
            bool isSharedUser = story.SharedUsers.Any(su => su.UserId == userId);

            if (!isOwner && !isSharedUser)
                return Unauthorized(new { message = "無權限修改此故事" });

            // 共享者只能修改標題和描述
            story.Title = updatedStory.Title;
            story.Description = updatedStory.Description;
            story.UpdatedAt = DateTime.UtcNow;

            // 只有創建者才能修改公開狀態
            if (isOwner)
            {
                story.IsPublic = updatedStory.IsPublic;
            }

            await _context.SaveChangesAsync();
            return Ok(story);
        }



        // 刪除故事（軟刪除）
        [Authorize]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteStory(int id)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
            var story = await _context.Stories.FindAsync(id);

            if (story == null || story.CreatorId != userId)
                return Unauthorized(new { message = "無權限刪除此故事" });

            story.DeletedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Ok(new { message = "故事已刪除" });
        }

        // 清理已刪除的故事（僅限管理員）
        [Authorize(Roles = "Admin")] // 只有管理員可以手動執行
        [HttpPost("cleanup-stories")]
        public async Task<IActionResult> CleanupDeletedStories()
        {
            int cleanupDays = int.TryParse(Environment.GetEnvironmentVariable("CLEANUP_THRESHOLD_DAYS"), out int days) ? days : 30;
            var thresholdDate = DateTime.UtcNow.AddDays(-cleanupDays);

            var expiredStories = await _context.Stories
                .Where(s => s.DeletedAt != null && s.DeletedAt < thresholdDate)
                .ToListAsync();

            if (!expiredStories.Any())
            {
                return Ok(new { message = "沒有過期的故事需要刪除。" });
            }

            _context.Stories.RemoveRange(expiredStories);
            await _context.SaveChangesAsync();

            return Ok(new { message = $"已刪除 {expiredStories.Count} 個過期故事。" });
        }


        // 分享修改故事權限給其他使用者
        [Authorize]
        [HttpPost("{id}/share/{targetUserId}")]
        public async Task<IActionResult> ShareStory(int id, int targetUserId)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
            var story = await _context.Stories.FindAsync(id);

            // 阻止使用者分享不存在的故事
            if (story == null || story.CreatorId != userId)
                return Unauthorized(new { message = "無權限共享此故事" });

            // 阻止使用者將自己的故事分享給自己
            if (userId == targetUserId)
                return BadRequest(new { message = "不能將故事共享給自己" });

            var existingShare = await _context.StorySharedUsers
                .FirstOrDefaultAsync(su => su.StoryId == id && su.UserId == targetUserId);

            if (existingShare == null)
            {
                var sharedStory = new StorySharedUser
                {
                    StoryId = id,
                    UserId = targetUserId
                };

                _context.StorySharedUsers.Add(sharedStory);
                await _context.SaveChangesAsync();

                // 檢查 ID 是否正確產生
                if (sharedStory.Id == 0)
                {
                    throw new Exception("ID 仍然為 0，可能 EF Core 沒有正確處理。");
                }
            }

            return Ok(new { message = "故事已共享" });
        }


        // 取消共享故事
        [Authorize]
        [HttpDelete("{id}/unshare/{targetUserId}")]
        public async Task<IActionResult> UnshareStory(int id, int targetUserId)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
            var story = await _context.Stories.Include(s => s.SharedUsers).FirstOrDefaultAsync(s => s.Id == id);

            if (story == null)
                return NotFound(new { message = "找不到故事" });

            // 允許創建者或被共享者取消共享
            if (story.CreatorId != userId && !story.SharedUsers.Any(su => su.UserId == userId))
                return Unauthorized(new { message = "無權限取消共享此故事" });

            var share = await _context.StorySharedUsers.FirstOrDefaultAsync(su => su.StoryId == id && su.UserId == targetUserId);
            if (share != null)
            {
                _context.StorySharedUsers.Remove(share);
                await _context.SaveChangesAsync();
            }

            return Ok(new { message = "已取消共享" });
        }

        // 取得公開故事內容（不需登入）
        [HttpGet("public/{publicId}")]
        public async Task<IActionResult> GetPublicStory(Guid publicId)
        {
            var story = await _context.Stories.FirstOrDefaultAsync(s => s.PublicId == publicId && s.IsPublic);
            if (story == null)
                return NotFound(new { message = "找不到該公開故事" });

            return Ok(story);
        }
    }
}
