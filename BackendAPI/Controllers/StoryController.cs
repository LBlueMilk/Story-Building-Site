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

        // 取得使用者所有故事（包含共享的）
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> GetStories()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);

            var stories = await _context.Stories
                .Where(s => s.CreatorId == userId || s.SharedUsers.Any(su => su.UserId == userId))
                .ToListAsync();

            return Ok(stories);
        }

        // 新增故事
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> CreateStory([FromBody] Story story)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
            story.CreatorId = userId;
            story.CreatedAt = DateTime.UtcNow;
            story.PublicId = Guid.NewGuid(); // 生成公開 ID

            _context.Stories.Add(story);
            await _context.SaveChangesAsync();

            return Ok(story);
        }

        // 更新故事
        [Authorize]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateStory(int id, [FromBody] Story updatedStory)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
            var story = await _context.Stories.Include(s => s.SharedUsers).FirstOrDefaultAsync(s => s.Id == id);

            if (story == null || (story.CreatorId != userId && !story.SharedUsers.Any(su => su.UserId == userId)))
                return Unauthorized(new { message = "無權限修改此故事" });

            story.Title = updatedStory.Title;
            story.Description = updatedStory.Description;
            story.UpdatedAt = DateTime.UtcNow;

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

        // 分享故事給其他使用者
        [Authorize]
        [HttpPost("{id}/share/{targetUserId}")]
        public async Task<IActionResult> ShareStory(int id, int targetUserId)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
            var story = await _context.Stories.FindAsync(id);

            if (story == null || story.CreatorId != userId)
                return Unauthorized(new { message = "無權限分享此故事" });

            var existingShare = await _context.StorySharedUsers
                .FirstOrDefaultAsync(su => su.StoryId == id && su.UserId == targetUserId);

            if (existingShare == null)
            {
                _context.StorySharedUsers.Add(new StorySharedUser { StoryId = id, UserId = targetUserId });
                await _context.SaveChangesAsync();
            }

            return Ok(new { message = "故事已分享" });
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
