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

            // 檢查使用者是否有權限存取該故事
            if (!await _userService.HasAccessToStoryAsync(userId, storyId))
                return Forbid();
            // 從儲存服務（SmartStorageService）讀取畫布 JSON
            var json = await _storageService.GetCanvasJsonAsync(storyId, userId);

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

            // 檢查使用者是否有權限存取該故事
            if (!await _userService.HasAccessToStoryAsync(userId, storyId))
                return Forbid();

            string jsonString = JsonSerializer.Serialize(dto.Json);

            // 儲存畫布資料（會寫入 Google Sheets 或 PostgreSQL，視使用者身份而定）
            await _storageService.SaveCanvasJsonAsync(storyId, userId, jsonString);
            return Ok(new { message = "Canvas saved." });
        }
    }
}
