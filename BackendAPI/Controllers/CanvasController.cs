using BackendAPI.Application.DTOs;
using BackendAPI.Services.GoogleSheets;
using BackendAPI.Services.Storage;
using BackendAPI.Services.User;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace BackendAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CanvasController : ControllerBase
    {
        private readonly IStorageService _storageService;
        private readonly IUserService _userService;

        public CanvasController(IStorageService storageService, IUserService userService)
        {
            _storageService = storageService;
            _userService = userService;
        }

        [HttpGet("{storyId}")]
        public async Task<IActionResult> GetCanvas(int storyId)
        {
            var userIdClaim = User.FindFirst("id");
            if (userIdClaim == null)
                return Unauthorized(new { error = "Missing user ID in token." });

            int userId = int.Parse(userIdClaim.Value);

            if (!await _userService.HasAccessToStoryAsync(userId, storyId))
                return Forbid();

            var json = await _storageService.GetCanvasJsonAsync(storyId, userId);
            return Ok(new { json });
        }


        [HttpPost("{storyId}")]
        public async Task<IActionResult> SaveCanvas(int storyId, [FromBody] JsonDataDto dto)
        {
            int userId = int.Parse(User.FindFirst("id")!.Value);

            // 檢查是否有權限修改此故事
            if (!await _userService.HasAccessToStoryAsync(userId, storyId))
            {
                return Forbid();
            }

            // 將傳入物件序列化為字串
            string jsonString = JsonSerializer.Serialize(dto.Json);
            await _storageService.SaveCanvasJsonAsync(storyId, userId, jsonString);
            return Ok(new { message = "Canvas saved." });
        }
    }
}
