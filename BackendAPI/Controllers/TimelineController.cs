using BackendAPI.Application.DTOs;
using BackendAPI.Services.GoogleSheets;
using BackendAPI.Services.Storage;
using BackendAPI.Services.User;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace BackendAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class TimelineController : ControllerBase
    {
        private readonly IStorageService _storageService;
        private readonly IUserService _userService;

        public TimelineController(IStorageService storageService, IUserService userService)
        {
            _storageService = storageService;
            _userService = userService;
        }

        // 取得指定故事的時間軸資料
        [HttpGet("{storyId}")]
        public async Task<IActionResult> GetTimeline(int storyId)
        {
            int userId;

            try
            {
                userId = _userService.GetUserId();
            }
            catch
            {
                return Unauthorized(new { error = "Missing or invalid token." });
            }
            // 檢查使用者是否擁有此故事的存取權限
            if (!await _userService.HasAccessToStoryAsync(userId, storyId))
                return Forbid();
            // 從儲存服務讀取時間軸資料
            var json = await _storageService.GetTimelineJsonAsync(storyId, userId);
            return Ok(new { json });
        }

        // 儲存或更新指定故事的時間軸資料
        [HttpPost("{storyId}")]
        public async Task<IActionResult> SaveTimeline(int storyId, [FromBody] JsonDataDto dto)
        {
            int userId;

            try
            {
                userId = _userService.GetUserId();
            }
            catch
            {
                return Unauthorized();
            }
            // 檢查使用者是否擁有此故事的存取權限
            if (!await _userService.HasAccessToStoryAsync(userId, storyId))
                return Forbid();
            // 將 JSON 物件序列化為字串
            string jsonString = JsonSerializer.Serialize(dto.Json);
            // 儲存時間軸資料
            await _storageService.SaveTimelineJsonAsync(storyId, userId, jsonString);

            return Ok(new { message = "Timeline saved." });
        }
    }
}
