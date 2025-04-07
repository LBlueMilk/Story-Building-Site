using BackendAPI.Application.DTOs;
using BackendAPI.Services.GoogleSheets;
using BackendAPI.Services.Storage;
using BackendAPI.Services.User;
using Google.Apis.Sheets.v4.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text.Json;

namespace BackendAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class CanvasController : ControllerBase
    {
        private readonly IStorageService _storageService;
        private readonly IUserService _userService;

        public CanvasController(IStorageService storageService, IUserService userService)
        {
            _storageService = storageService;
            _userService = userService;
        }

        // 取得指定故事的畫布資料
        [HttpGet("{storyId}")]
        public async Task<IActionResult> GetCanvas(int storyId)
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

            // 檢查故事是否存在
            if (!await _userService.StoryExistsAsync(storyId))
                return NotFound(new { error = "Story not found." });

            // 檢查使用者是否有權限存取該故事
            if (!await _userService.HasAccessToStoryAsync(userId, storyId))
                return Forbid();

            // 從儲存服務讀取畫布 JSON 資料
            var jsonString = await _storageService.GetCanvasJsonAsync(storyId, userId);
            // 將 JSON 字串轉為物件
            var json = JsonDocument.Parse(jsonString).RootElement;

            return Ok(new { json });
        }

        // 儲存或更新指定故事的畫布資料
        [HttpPost("{storyId}")]
        public async Task<IActionResult> SaveCanvas(int storyId, [FromBody] JsonDataDto dto)
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

            // 檢查故事是否存在
            if (!await _userService.StoryExistsAsync(storyId))
                return NotFound(new { error = "Story not found." });

            // 檢查使用者是否有權限存取該故事
            if (!await _userService.HasAccessToStoryAsync(userId, storyId))
                return Forbid();

            string jsonString;

            // 若傳入為已序列化的 JSON 字串，直接取出
            if (dto.Json.ValueKind == JsonValueKind.String)
                jsonString = dto.Json.GetString()!;
            else
                jsonString = dto.Json.GetRawText();


            // 儲存畫布資料（會寫入 Google Sheets 或 PostgreSQL，視使用者身份而定）
            await _storageService.SaveCanvasJsonAsync(storyId, userId, jsonString, DateTime.UtcNow);
            return Ok(new { message = "Canvas saved." });
        }
    }
}
