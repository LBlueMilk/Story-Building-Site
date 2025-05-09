﻿using BackendAPI.Application.DTOs;
using BackendAPI.Services.GoogleSheets;
using BackendAPI.Services.Storage;
using BackendAPI.Services.User;
using BackendAPI.Utils;
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

            // 檢查故事是否存在
            if (!await _userService.StoryExistsAsync(storyId))
                return NotFound(new { error = "Story not found." });

            // 檢查使用者是否擁有此故事的存取權限
            if (!await _userService.HasAccessToStoryAsync(userId, storyId))
                return Forbid();

            // 從儲存服務讀取時間軸 JSON 資料
            var result = await _storageService.GetTimelineWithLastModifiedAsync(storyId, userId);

            // 若查無資料，視為尚未建立，回傳空事件與年號欄位
            string jsonString = result?.Json ?? "{\"events\":[],\"eras\":[]}";
            string lastModified = result?.LastModifiedRaw ?? DateTime.UtcNow.ToString("o");

            // 將 JSON 字串轉為物件
            var json = JsonDocument.Parse(jsonString).RootElement;

            return Ok(new
            {
                json,
                lastModified
            });
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

            // 檢查故事是否存在
            if (!await _userService.StoryExistsAsync(storyId))
                return NotFound(new { error = "Story not found." });

            // 檢查使用者是否擁有此故事的存取權限
            if (!await _userService.HasAccessToStoryAsync(userId, storyId))
                return Forbid();

            string jsonString;

            // 若傳入為已序列化的 JSON 字串，直接取出
            if (dto.Json.ValueKind == JsonValueKind.String)
                jsonString = dto.Json.GetString()!;
            else
                jsonString = dto.Json.GetRawText();

            // 確保 JSON 字串符合事件 ID 的格式
            jsonString = JsonSanitizer.EnsureEventIds(jsonString);

            // 儲存時間軸資料（會寫入 Google Sheets 或 PostgreSQL，視使用者身份而定）
            await _storageService.SaveTimelineJsonAsync(storyId, userId, jsonString, DateTime.UtcNow);
            try
            {
                var parsedJson = JsonDocument.Parse(jsonString).RootElement;

                return Ok(new
                {
                    success = true,
                    message = "Timeline saved.",
                    json = parsedJson
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    error = "無法解析 JSON，請檢查內容格式是否正確",
                    detail = ex.Message,
                    jsonRaw = jsonString // ✅ 可選：方便 Debug
                });
            }
        }
    }
}
