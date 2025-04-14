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

            // 檢查故事是否存在
            if (!await _userService.StoryExistsAsync(storyId))
                return NotFound(new { error = "Story not found." });

            // 檢查使用者是否有存取該故事的權限
            if (!await _userService.HasAccessToStoryAsync(userId, storyId))
                return Forbid();

            // 從儲存服務讀取角色 JSON 資料
            var result = await _storageService.GetCharacterWithLastModifiedAsync(storyId, userId);
            if (result == null)
                return NotFound(new { error = "Character not found." });

            // 將 JSON 字串轉為物件
            var json = JsonDocument.Parse(result.Json).RootElement;

            return Ok(new
            {
                json,
                lastModified = DateTime.Parse(result.LastModifiedRaw).ToString("o")
            });
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

            // 檢查故事是否存在
            if (!await _userService.StoryExistsAsync(storyId))
                return NotFound(new { error = "Story not found." });

            // 檢查是否有操作權限
            if (!await _userService.HasAccessToStoryAsync(userId, storyId))
                return Forbid();


            try
            {
                JsonElement root;

                if (dto.Json.ValueKind == JsonValueKind.String)
                {
                    root = JsonDocument.Parse(dto.Json.GetString()!).RootElement;
                }
                else
                {
                    root = dto.Json;
                }

                if (!root.TryGetProperty("characters", out var characters))
                    return BadRequest(new { error = "Missing 'characters' property." });


                var fixedCharacters = new List<JsonElement>();

                foreach (var character in characters.EnumerateArray())
                {
                    using var doc = JsonDocument.Parse(character.GetRawText());
                    var obj = doc.RootElement.EnumerateObject().ToDictionary(p => p.Name, p => p.Value);

                    if (!obj.ContainsKey("attributes"))
                        obj["attributes"] = JsonDocument.Parse("{}").RootElement;

                    if (!obj.ContainsKey("relations"))
                        obj["relations"] = JsonDocument.Parse("[]").RootElement;

                    var fixedJson = JsonSerializer.SerializeToElement(obj);
                    fixedCharacters.Add(fixedJson);
                }

                // 包成 characters 陣列物件
                var payload = new Dictionary<string, object>
                {
                    ["characters"] = fixedCharacters
                };

                var jsonString = JsonSerializer.Serialize(payload);



                await _storageService.SaveCharacterJsonAsync(storyId, userId, jsonString, DateTime.UtcNow);
                return Ok(new { message = "Characters saved." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to save character data.", detail = ex.Message });
            }
        }
    }

}
