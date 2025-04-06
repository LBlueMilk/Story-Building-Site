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
    public class CharacterController : ControllerBase
    {
        private readonly IStorageService _storageService;
        private readonly IUserService _userService;

        public CharacterController(IStorageService storageService, IUserService userService)
        {
            _storageService = storageService;
            _userService = userService;
        }

        // 取得指定故事的角色資料
        [HttpGet("{storyId}")]
        public async Task<IActionResult> GetCharacters(int storyId)
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

            // 檢查使用者是否有存取該故事的權限
            if (!await _userService.HasAccessToStoryAsync(userId, storyId))
                return Forbid();

            // 從儲存服務讀取角色 JSON 資料
            var json = await _storageService.GetCharacterJsonAsync(storyId, userId);
            return Ok(new { json });
        }

        // 儲存或更新指定故事的角色資料
        [HttpPost("{storyId}")]
        public async Task<IActionResult> SaveCharacters(int storyId, [FromBody] JsonDataDto dto)
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
            // 檢查是否有操作權限
            if (!await _userService.HasAccessToStoryAsync(userId, storyId))
                return Forbid();

            // 將傳入的 JSON 物件序列化為字串
            string jsonString = JsonSerializer.Serialize(dto.Json);
            // 儲存角色資料
            await _storageService.SaveCharacterJsonAsync(storyId, userId, jsonString);
            return Ok(new { message = "Characters saved." });
        }
    }

}
