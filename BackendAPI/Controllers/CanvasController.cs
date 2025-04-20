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
            if (!await _userService.StoryExistsAsync(storyId)) return NotFound(new { error = "Story not found." });

            // 檢查使用者是否有權限存取該故事
            if (!await _userService.HasAccessToStoryAsync(userId, storyId)) return Forbid();

            try
            {
                // 從儲存服務讀取畫布 JSON 資料
                var result = await _storageService.ReadCanvasChunksAsync(storyId.ToString(), userId.ToString());

                // 若查無資料，視為尚未建立，回傳空畫布
                string jsonString = string.IsNullOrWhiteSpace(result?.Json)
                    ? "{\"strokes\":[],\"images\":[],\"markers\":[],\"canvasMeta\":{\"width\":1920,\"height\":1080,\"scrollX\":0,\"scrollY\":0}}" // 預設值
                    : result.Json;

                string lastModified = result?.LastModifiedRaw ?? DateTime.UtcNow.ToString("o");

                JsonElement json;


                try
                {
                    // 將 JSON 字串轉為物件
                    json = JsonDocument.Parse(jsonString).RootElement;
                }
                catch (JsonException ex)
                {
                    return StatusCode(500, new
                    {
                        error = "Invalid canvas JSON format.",
                        detail = ex.Message
                    });
                }

                return Ok(new
                {
                    json,
                    lastModified
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    error = "Failed to retrieve canvas data.",
                    detail = ex.Message
                });
            }
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

            try
            {
                string jsonString = dto.Json.ValueKind == JsonValueKind.String
                    ? dto.Json.GetString()!
                    : dto.Json.GetRawText();

                Console.WriteLine("[CanvasController] 儲存接收到的 json:");
                Console.WriteLine(jsonString);


                // 儲存畫布資料（會寫入 Google Sheets 或 PostgreSQL，視使用者身份而定）
                await _storageService.SaveCanvasChunksAsync(storyId.ToString(), userId.ToString(), jsonString, DateTime.UtcNow);
                return Ok(new { message = "Canvas saved." });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CanvasController] 儲存錯誤: {ex.Message}");
                return StatusCode(500, new { error = "Failed to save canvas", detail = ex.Message });
            }
        }
    }
}
