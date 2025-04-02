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
    public class TimelineController : ControllerBase
    {
        private readonly IStorageService _storageService;
        private readonly IUserService _userService;

        public TimelineController(IStorageService storageService, IUserService userService)
        {
            _storageService = storageService;
            _userService = userService;
        }

        [HttpGet("{storyId}")]
        public async Task<IActionResult> GetTimeline(int storyId)
        {
            int userId = int.Parse(User.FindFirst("id")!.Value);
            if (!await _userService.HasAccessToStoryAsync(userId, storyId))
            {
                return Forbid();
            }
            var json = await _storageService.GetTimelineJsonAsync(storyId, userId);
            return Ok(new { json });
        }

        [HttpPost("{storyId}")]
        public async Task<IActionResult> SaveTimeline(int storyId, [FromBody] JsonDataDto dto)
        {
            int userId = int.Parse(User.FindFirst("id")!.Value);
            if (!await _userService.HasAccessToStoryAsync(userId, storyId))
            {
                return Forbid();
            }
            string jsonString = JsonSerializer.Serialize(dto.Json);
            await _storageService.SaveTimelineJsonAsync(storyId, userId, jsonString);
            return Ok(new { message = "Timeline saved." });
        }
    }
}
