using BackendAPI.Application.DTOs;
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

        // 取得使用者的所有故事（包含自己創建 & 共享給他的）但排除已刪除的
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> GetStories()
        {
            // 取得使用者 ID
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);

            // 查詢故事
            var stories = await _context.Stories
                .Where(s =>
                    s.DeletedAt == null &&  // 先確保沒有刪除
                    (s.CreatorId == userId || s.SharedUsers.Any(su => su.UserId == userId)) // 使用者擁有權限
                )
                .Select(s => new StoryResponseDto
                {
                    Id = s.Id,
                    PublicId = s.PublicId,
                    Title = s.Title,
                    Description = s.Description,
                    IsPublic = s.IsPublic,
                    CreatedAt = s.CreatedAt
                })
                .ToListAsync();

            return Ok(stories);
        }

        // 新增故事
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> CreateStory([FromBody] CreateStoryDto storyDto)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);

            // 檢查標題是否為空
            if (string.IsNullOrWhiteSpace(storyDto.Title))
            {
                return BadRequest(new { message = "故事標題不能為空" });
            }

            // 清理 Description（允許 null，但如果有值就 Trim）
            string? description = string.IsNullOrWhiteSpace(storyDto.Description) ? null : storyDto.Description.Trim();

            // 確保 IsPublic 有值，預設為 false
            bool isPublic = storyDto.IsPublic ?? false;

            // 創建新故事
            var newStory = new Story
            {
                CreatorId = userId,
                Title = storyDto.Title.Trim(),  // 確保標題沒有前後空格
                Description = description,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,  // 初始化 UpdatedAt
                PublicId = Guid.NewGuid(),
                IsPublic = isPublic
            };

            _context.Stories.Add(newStory);
            await _context.SaveChangesAsync();

            // 回傳新故事的 Response
            var response = new StoryResponseDto
            {
                Id = newStory.Id,
                PublicId = newStory.PublicId,
                Title = newStory.Title,
                Description = newStory.Description,
                IsPublic = newStory.IsPublic,
                CreatedAt = newStory.CreatedAt,
                UpdatedAt = newStory.UpdatedAt
            };

            return Ok(response);
        }

        // 更新故事
        [Authorize]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateStory(int id, [FromBody] UpdateStoryDto updatedStory)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);

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

            // 記錄是否有變更
            bool hasChanges = false;

            // 修改標題與描述（共享者 & 創建者都能修改）
            if (!string.IsNullOrWhiteSpace(updatedStory.Title) && updatedStory.Title != story.Title)
            {
                story.Title = updatedStory.Title;
                hasChanges = true;
            }

            if (updatedStory.Description != null && updatedStory.Description != story.Description)
            {
                story.Description = updatedStory.Description;
                hasChanges = true;
            }

            // 只有創建者才能修改公開狀態
            if (isOwner && updatedStory.IsPublic.HasValue && updatedStory.IsPublic != story.IsPublic)
            {
                story.IsPublic = updatedStory.IsPublic.Value;
                hasChanges = true;
            }

            story.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            // 回傳更新後的故事資訊
            var response = new StoryResponseDto
            {
                Id = story.Id,
                PublicId = story.PublicId,
                Title = story.Title,
                Description = story.Description,
                IsPublic = story.IsPublic,
                CreatedAt = story.CreatedAt,
                UpdatedAt = story.UpdatedAt
            };

            return Ok(response);
        }

        // 刪除故事（軟刪除）
        [Authorize]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteStory(int id)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
            var story = await _context.Stories.FindAsync(id);

            if (story == null)
                return NotFound(new { message = "找不到故事" });

            if (story.CreatorId != userId)
                return Unauthorized(new { message = "無權限刪除此故事" });

            if (story.DeletedAt != null)
                return BadRequest(new { message = "此故事已經被刪除" });

            story.DeletedAt = DateTime.UtcNow;
            story.RestoredAt = null; // 確保恢復時間清空
            await _context.SaveChangesAsync();

            return Ok(new { message = "故事已刪除" });
        }


        // 取得使用者已刪除的故事（軟刪除）
        [Authorize]
        [HttpGet("deleted")]
        public async Task<IActionResult> GetDeletedStories()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);

            var deletedStories = await _context.Stories
                .Where(s =>
                    s.DeletedAt != null &&  // 只查找已刪除的
                    s.CreatorId == userId   // 只顯示自己刪除的故事
                )
                .Select(s => new StoryResponseDto
                {
                    Id = s.Id,
                    PublicId = s.PublicId,
                    Title = s.Title,
                    Description = s.Description,
                    IsPublic = s.IsPublic,
                    CreatedAt = s.CreatedAt,
                    DeletedAt = s.DeletedAt // 顯示刪除時間，方便前端管理
                })
                .ToListAsync();

            if (!deletedStories.Any())
                return Ok(new { message = "沒有已刪除的故事" });

            return Ok(deletedStories);
        }


        // 恢復故事（軟刪除恢復）
        [Authorize]
        [HttpPost("restore/{id}")]
        public async Task<IActionResult> RestoreStory(int id)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
            var story = await _context.Stories.FindAsync(id);

            if (story == null || story.CreatorId != userId)
                return Unauthorized(new { message = "無權限恢復此故事" });

            if (story.DeletedAt == null)
                return BadRequest(new { message = "此故事未被刪除，無需恢復" });

            story.DeletedAt = null; // 移除刪除標記
            story.RestoredAt = DateTime.UtcNow; // 記錄恢復時間
            await _context.SaveChangesAsync();

            return Ok(new { message = "故事已恢復" });
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

        // 取得單一故事（確保使用者有權限存取）
        [Authorize]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetStory(int id)
        {
            // 取得使用者 ID
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);

            // 查詢故事
            var story = await _context.Stories
                .Where(s => s.DeletedAt == null) // 過濾已刪除
                .FirstOrDefaultAsync(s =>
                    s.Id == id &&
                    (s.CreatorId == userId || s.IsPublic || s.SharedUsers.Any(su => su.UserId == userId))
                );

            if (story == null)
                return NotFound(new { message = "找不到故事，或您沒有權限查看" });

            return Ok(new StoryResponseDto
            {
                Id = story.Id,
                PublicId = story.PublicId,
                Title = story.Title,
                Description = story.Description,
                IsPublic = story.IsPublic,
                CreatedAt = story.CreatedAt
            });
        }

        [Authorize]
        [HttpPost("{id}/share/by-code/{userCode}")]
        public async Task<IActionResult> ShareStoryByCode(int id, string userCode)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
            var story = await _context.Stories.FindAsync(id);

            if (story == null || story.CreatorId != userId)
                return Unauthorized(new { message = "無權限共享此故事" });

            // 查找使用者的 ID
            var targetUser = await _context.Users.FirstOrDefaultAsync(u => u.UserCode == userCode);
            if (targetUser == null)
                return NotFound(new { message = "找不到該識別碼的使用者" });

            // 確保不會重複分享
            bool alreadyShared = await _context.StorySharedUsers
                .AnyAsync(su => su.StoryId == id && su.UserId == targetUser.Id);

            if (alreadyShared)
                return BadRequest(new { message = "已經共享過此故事" });

            var sharedStory = new StorySharedUser
            {
                StoryId = id,
                UserId = targetUser.Id
            };

            _context.StorySharedUsers.Add(sharedStory);
            await _context.SaveChangesAsync();

            return Ok(new { message = $"已成功共享故事給 {userCode}" });
        }

        // 產生共享連結
        [Authorize]
        [HttpPost("{id}/generate-link")]
        public async Task<IActionResult> GenerateShareLink(int id)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
            var story = await _context.Stories.FindAsync(id);

            if (story == null || story.CreatorId != userId)
                return Unauthorized(new { message = "無權限產生共享連結" });

            // 產生新的 Token 並設定 10 分鐘有效期
            story.ShareToken = Guid.NewGuid().ToString();
            story.ShareTokenExpiresAt = DateTime.UtcNow.AddMinutes(10);
            await _context.SaveChangesAsync();

            string shareLink = $"https://your-frontend.com/share/{story.ShareToken}";

            return Ok(new { shareLink, expiresAt = story.ShareTokenExpiresAt });
        }


        // 接受共享連結
        [HttpGet("shared/{token}")]
        public async Task<IActionResult> AccessSharedStory(string token)
        {
            var story = await _context.Stories
                .FirstOrDefaultAsync(s => s.ShareToken == token);

            if (story == null || story.ShareTokenExpiresAt == null)
                return NotFound(new { message = "無效的共享連結" });

            // 檢查是否過期
            if (story.ShareTokenExpiresAt < DateTime.UtcNow)
            {
                // 過期後刪除 Token
                story.ShareToken = null;
                story.ShareTokenExpiresAt = null;
                await _context.SaveChangesAsync();
                return BadRequest(new { message = "共享連結已過期" });
            }

            // 使用後立即失效
            story.ShareToken = null;
            story.ShareTokenExpiresAt = null;
            await _context.SaveChangesAsync();

            return Ok(new StoryResponseDto
            {
                Id = story.Id,
                PublicId = story.PublicId,
                Title = story.Title,
                Description = story.Description,
                IsPublic = story.IsPublic,
                CreatedAt = story.CreatedAt
            });
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
