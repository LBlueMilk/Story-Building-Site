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

        // 取得使用者的所有故事（自己創建）但排除已刪除的
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> GetStories()
        {
            // 取得使用者 ID
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);

            // 查詢故事
            var stories = await _context.Stories
                .Include(s => s.SharedUsers)
                    .ThenInclude(su => su.User)
                .Where(s =>
                    s.DeletedAt == null &&  // 先確保沒有刪除
                    s.CreatorId == userId
                )
                .Select(s => new StoryResponseDto
                {
                    Id = s.Id,
                    PublicId = s.PublicId,
                    Title = s.Title,
                    Description = s.Description,
                    IsPublic = s.IsPublic,
                    CreatedAt = s.CreatedAt,
                    sharedUsers = s.SharedUsers.Select(u => new SharedUserDto
                    {
                        Email = u.User.Email,
                        Name = u.User.Name
                    }).ToList()
                })
                .ToListAsync();

            return Ok(stories);
        }

        // 自己分享&別人分享 給自己的故事
        [Authorize]
        [HttpGet("shared/all")]
        public async Task<IActionResult> GetAllSharedStories()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);

            var stories = await _context.Stories
                .Include(s => s.Creator)
                .Include(s => s.SharedUsers)
                    .ThenInclude(su => su.User)
                .Where(s =>
                    s.DeletedAt == null &&
                    (
                        // 自己分享出去的故事（自己是創建者 & 有分享對象）
                        (s.CreatorId == userId && s.SharedUsers.Any()) ||

                        // 別人分享給我的故事（自己不是創建者，但在共享名單內）
                        (s.CreatorId != userId && s.SharedUsers.Any(su => su.UserId == userId))
                    )
                )
                .Select(s => new StoryResponseDto
                {
                    Id = s.Id,
                    PublicId = s.PublicId,
                    Title = s.Title,
                    Description = s.Description,
                    IsPublic = s.IsPublic,
                    CreatedAt = s.CreatedAt,
                    sharedUsers = s.SharedUsers.Select(u => new SharedUserDto
                    {
                        Email = u.User.Email,
                        Name = u.User.Name
                    }).ToList(),
                    CreatorId = s.CreatorId,
                    CreatorName = s.Creator.Name,
                    CreatorEmail = s.Creator.Email
                })
                .ToListAsync();

            return Ok(stories);
        }

        // 新增故事
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> CreateStory([FromBody] CreateStoryDto storyDto)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (userIdClaim == null || !int.TryParse(userIdClaim, out int userId))
                {
                    return Unauthorized(new { message = "無效的 Token" });
                }
                Console.WriteLine($"userId: {userId}");

                // 檢查標題
                if (string.IsNullOrWhiteSpace(storyDto.Title))
                {
                    return BadRequest(new { message = "故事標題不能為空" });
                }

                string? description = string.IsNullOrWhiteSpace(storyDto.Description) ? null : storyDto.Description.Trim();
                bool isPublic = storyDto.IsPublic ?? false;

                var newStory = new Story
                {
                    CreatorId = userId,
                    Title = storyDto.Title.Trim(),
                    Description = description,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    PublicId = Guid.NewGuid(),
                    IsPublic = isPublic
                };

                _context.Stories.Add(newStory);
                await _context.SaveChangesAsync();

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
            catch (Exception ex)
            {
                Console.WriteLine($"CreateStory 發生錯誤: {ex.Message}");
                return StatusCode(500, new { message = "伺服器內部錯誤，請稍後再試" });
            }
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

            // 若無任何變更，則直接回傳，避免不必要的寫入
            if (!hasChanges)
            {
                return Ok(new { message = "未檢測到變更，無需更新" });
            }

            // 只有有變更時才更新
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

            return Ok(new { message = "故事更新成功", story = response });
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

        // 共享故事給其他使用者
        [Authorize]
        [HttpPost("{id}/share/by-code/{userCode}")]
        public async Task<IActionResult> ShareStoryByCode(int id, string userCode)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);

            var storyWithTargetUser = await _context.Stories
                .Where(s => s.Id == id && s.CreatorId == userId) // 確保使用者有權限
                .Select(s => new
                {
                    Story = s,
                    TargetUser = _context.Users.FirstOrDefault(u => u.UserCode == userCode) // 查找 UserCode
                })
                .FirstOrDefaultAsync();

            // 確保故事存在且使用者有權限
            if (storyWithTargetUser == null)
                return Unauthorized(new { message = "無權限共享此故事或故事不存在" });

            // 驗證目標使用者是否存在
            if (storyWithTargetUser.TargetUser == null)
                return NotFound(new { message = "找不到該識別碼的使用者" });

            var targetUserId = storyWithTargetUser.TargetUser.Id;

            // 禁止自己分享給自己
            if (targetUserId == userId)
                return BadRequest(new { message = "無法將故事分享給自己" });

            // 檢查是否已經共享過
            bool alreadyShared = await _context.StorySharedUsers.AnyAsync(su => su.StoryId == id && su.UserId == targetUserId);
            if (alreadyShared)
                return BadRequest(new { message = "已經共享過此故事" });

            // 新增共享紀錄
            _context.StorySharedUsers.Add(new StorySharedUser
            {
                StoryId = id,
                UserId = targetUserId
            });

            await _context.SaveChangesAsync();
            return Ok(new
            {
                message = $"已成功共享故事給 {storyWithTargetUser.TargetUser.Name}",
                sharedUser = new
                {
                    storyWithTargetUser.TargetUser.Id,
                    storyWithTargetUser.TargetUser.Name,
                    storyWithTargetUser.TargetUser.Email
                }
            });
        }

        // 產生共享連結（允許在 Token 被使用後立即重新產生）
        [Authorize]
        [HttpPost("{id}/generate-link")]
        public async Task<IActionResult> GenerateShareLink(int id)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
            var story = await _context.Stories.FindAsync(id);

            if (story == null || story.CreatorId != userId)
                return Unauthorized(new { message = "無權限產生共享連結" });

            // 如果 Token 已被使用（被清空），允許立刻產生新 Token
            if (story.ShareToken == null || story.ShareTokenExpiresAt == null || story.ShareTokenExpiresAt < DateTime.UtcNow)
            {
                story.ShareToken = Guid.NewGuid().ToString();
                story.ShareTokenExpiresAt = DateTime.UtcNow.AddMinutes(10);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    shareLink = $"https://your-frontend.com/share/{story.ShareToken}",
                    expiresAt = story.ShareTokenExpiresAt
                });
            }

            // Token 仍然有效，不允許重新產生
            return BadRequest(new
            {
                message = "共享連結仍然有效，無需重新產生",
                shareLink = $"https://your-frontend.com/share/{story.ShareToken}",
                expiresAt = story.ShareTokenExpiresAt
            });
        }


        // 接受共享連結
        [Authorize] // 需要登入才能存取共享內容
        [HttpGet("shared/{token}")]
        public async Task<IActionResult> AccessSharedStory(string token)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);

            var story = await _context.Stories
                .Include(s => s.SharedUsers)
                .FirstOrDefaultAsync(s => s.ShareToken == token);

            if (story == null || story.ShareTokenExpiresAt == null)
                return NotFound(new { message = "無效的共享連結" });

            // 檢查 Token 是否過期
            if (story.ShareTokenExpiresAt < DateTime.UtcNow)
            {
                // 過期後清除 Token，防止重複使用
                story.ShareToken = null;
                story.ShareTokenExpiresAt = null;
                await _context.SaveChangesAsync(); // 只有過期時才存入 DB
                return BadRequest(new { message = "共享連結已過期" });
            }

            // 檢查當前登入使用者是否已經擁有該故事的共享權限
            bool alreadyShared = story.CreatorId == userId ||
                                 story.SharedUsers.Any(su => su.UserId == userId);

            if (alreadyShared)
            {
                return Ok(new
                {
                    message = "你已經擁有該故事的共享權限",
                    story = new StoryResponseDto
                    {
                        Id = story.Id,
                        PublicId = story.PublicId,
                        Title = story.Title,
                        Description = story.Description,
                        IsPublic = story.IsPublic,
                        CreatedAt = story.CreatedAt
                    }
                });
            }

            // 新增共享權限
            var sharedUser = new StorySharedUser
            {
                StoryId = story.Id,
                UserId = userId
            };

            _context.StorySharedUsers.Add(sharedUser);

            // 使 Token 失效，防止多次共享
            story.ShareToken = null;
            story.ShareTokenExpiresAt = null;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "你已成功獲得該故事的共享權限",
                story = new StoryResponseDto
                {
                    Id = story.Id,
                    PublicId = story.PublicId,
                    Title = story.Title,
                    Description = story.Description,
                    IsPublic = story.IsPublic,
                    CreatedAt = story.CreatedAt
                }
            });
        }


        // 取消共享故事（創建者 & 共享者皆可取消）
        [Authorize]
        [HttpDelete("{id}/unshare/{targetUserId}")]
        public async Task<IActionResult> UnshareStory(int id, int targetUserId)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
            var story = await _context.Stories
                .Include(s => s.SharedUsers)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (story == null)
                return NotFound(new { message = "找不到故事" });

            // 允許創建者或該共享者自己移除共享
            if (story.CreatorId != userId && targetUserId != userId)
                return Unauthorized(new { message = "無權限取消共享" });

            var share = await _context.StorySharedUsers
                .FirstOrDefaultAsync(su => su.StoryId == id && su.UserId == targetUserId);

            if (share == null)
                return BadRequest(new { message = "該使用者未被共享此故事" });

            _context.StorySharedUsers.Remove(share);
            await _context.SaveChangesAsync();

            return Ok(new { message = "已成功取消共享" });
        }

        // 取得公開故事列表
        [HttpGet("public/{publicId}")]
        public async Task<IActionResult> GetPublicStory(Guid publicId)
        {
            var story = await _context.Stories
                .Where(s => s.IsPublic && s.PublicId == publicId)
                .Select(s => new
                {
                    s.PublicId,
                    s.Title,
                    s.Description,
                    Creator = new { s.Creator.Name }, // 只回傳創建者的名字，不回傳 ID
                    s.CreatedAt
                })
                .FirstOrDefaultAsync();

            if (story == null)
                return NotFound(new { message = "找不到該公開故事" });

            return Ok(story);
        }


        // 切換故事公開狀態
        [Authorize] // 需要登入
        [HttpPut("{id}/toggle-public")]
        public async Task<IActionResult> TogglePublicStory(int id)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
            var story = await _context.Stories.FindAsync(id);

            if (story == null || story.CreatorId != userId)
                return Unauthorized(new { message = "無權限更改此故事的公開狀態" });

            // 切換 IsPublic 狀態
            story.IsPublic = !story.IsPublic;
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = $"故事公開狀態已變更為 {(story.IsPublic ? "公開" : "私人")}",
                isPublic = story.IsPublic
            });
        }

    }
}
