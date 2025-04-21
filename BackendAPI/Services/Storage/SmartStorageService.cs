using BackendAPI.Application.DTOs;
using BackendAPI.Services.GoogleSheets;
using BackendAPI.Services.User;

namespace BackendAPI.Services.Storage
{
    public class SmartStorageService : IStorageService
    {
        private readonly IStoryDataService _storyDataService;
        private readonly CanvasSheetService _canvasSheetService;
        private readonly CharacterSheetService _characterSheetService;
        private readonly TimelineSheetService _timelineSheetService;
        private readonly IUserService _userService;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public SmartStorageService(
            IStoryDataService storyDataService,
            CanvasSheetService canvasSheetService,
            CharacterSheetService characterSheetService,
            TimelineSheetService timelineSheetService,
            IUserService userService,
            IHttpContextAccessor httpContextAccessor)
        {
            _storyDataService = storyDataService;
            _canvasSheetService = canvasSheetService;
            _characterSheetService = characterSheetService;
            _timelineSheetService = timelineSheetService;
            _userService = userService;
            _httpContextAccessor = httpContextAccessor;
        }

        private async Task<bool> IsGoogleUserAsync(int userId)
        {
            var provider = await _userService.GetUserProviderAsync(userId);
            return provider?.ToLower() == "google";
        }

        // ---------- Canvas ----------
        public async Task<string?> GetCanvasJsonAsync(int storyId, int userId)
        {
            if (await IsGoogleUserAsync(userId))
                return await _canvasSheetService.GetCanvasJsonAsync(storyId.ToString(), userId.ToString());
            else
                return await _storyDataService.GetCanvasJsonAsync(storyId);
        }

        public async Task SaveCanvasJsonAsync(int storyId, int userId, string json, DateTime lastModified)
        {
            if (await IsGoogleUserAsync(userId))
                await _canvasSheetService.SaveCanvasJsonAsync(storyId.ToString(), userId.ToString(), json, lastModified);
            else
                await _storyDataService.SaveCanvasJsonAsync(storyId, json); // PostgreSQL 無 lastModified 欄
        }

        public async Task<JsonWithModifiedDto?> GetCanvasWithLastModifiedAsync(int storyId, int userId)
        {
            if (await IsGoogleUserAsync(userId))
            {
                return await _canvasSheetService.GetCanvasWithLastModifiedAsync(
                    storyId.ToString(), userId.ToString());
            }
            else
            {
                var json = await _storyDataService.GetCanvasJsonAsync(storyId);
                return json == null ? null : new JsonWithModifiedDto
                {
                    Json = json,
                    LastModifiedRaw = DateTime.MinValue.ToString("o")  // PostgreSQL 無 lastModified 資訊
                };
            }
        }

        // Canvas 分段儲存（供大檔案使用）
        public async Task SaveCanvasChunksAsync(string storyId, string userId, string json, DateTime lastModified)
        {
            // 僅 Google 使用者才用 Sheets 儲存
            var userIdInt = int.Parse(userId);
            if (await IsGoogleUserAsync(userIdInt))
            {
                await _canvasSheetService.SaveCanvasChunksAsync(storyId, userId, json, lastModified);
            }
            else
            {
                // 非 Google 使用者（網站註冊帳號）→ 直接寫入 PostgreSQL
                await _storyDataService.SaveCanvasJsonAsync(int.Parse(storyId), json);
            }
        }

        // Canvas 分段讀取（合併所有 chunk 成為一筆資料）
        public async Task<JsonWithModifiedDto?> ReadCanvasChunksAsync(string storyId, string userId)
        {
            var userIdInt = int.Parse(userId);
            if (await IsGoogleUserAsync(userIdInt))
            {
                // Google 使用者 → 走 Sheets 分段邏輯
                return await _canvasSheetService.ReadCanvasChunksAsync(storyId, userId);
            }
            else
            {
                // 非 Google 使用者（網站註冊帳號）→ 從 PostgreSQL 讀取
                var json = await _storyDataService.GetCanvasJsonAsync(int.Parse(storyId));

                // 若為 null 或空白，給預設空白畫布資料
                var fixedJson = string.IsNullOrWhiteSpace(json)
                    ? "{\"strokes\":[],\"images\":[],\"markers\":[],\"canvasMeta\":{\"width\":1920,\"height\":1080,\"scrollX\":0,\"scrollY\":0}}"
                    : json;

                return new JsonWithModifiedDto
                {
                    Json = fixedJson,
                    LastModifiedRaw = DateTime.UtcNow.ToString("o")
                };
            }
        }



        // ---------- Character ----------
        public async Task<string?> GetCharacterJsonAsync(int storyId, int userId)
        {
            if (await IsGoogleUserAsync(userId))
                return await _characterSheetService.GetCharacterJsonAsync(storyId.ToString(), userId.ToString());
            else
                return await _storyDataService.GetCharacterJsonAsync(storyId);
        }

        public async Task SaveCharacterJsonAsync(int storyId, int userId, string json, DateTime lastModified)
        {
            if (await IsGoogleUserAsync(userId))
                await _characterSheetService.SaveCharacterJsonAsync(storyId.ToString(), userId.ToString(), json, lastModified);
            else
                await _storyDataService.SaveCharacterJsonAsync(storyId, json);
        }

        public async Task<JsonWithModifiedDto?> GetCharacterWithLastModifiedAsync(int storyId, int userId)
        {
            if (await IsGoogleUserAsync(userId))
            {
                return await _characterSheetService.GetCharacterWithLastModifiedAsync(
                    storyId.ToString(), userId.ToString());
            }
            else
            {
                var json = await _storyDataService.GetCharacterJsonAsync(storyId);
                return json == null ? null : new JsonWithModifiedDto
                {
                    Json = json,
                    LastModifiedRaw = DateTime.MinValue.ToString("o")
                };
            }
        }

        // ---------- Timeline ----------
        public async Task<string?> GetTimelineJsonAsync(int storyId, int userId)
        {
            if (await IsGoogleUserAsync(userId))
                return await _timelineSheetService.GetTimelineJsonAsync(storyId.ToString(), userId.ToString());
            else
                return await _storyDataService.GetTimelineJsonAsync(storyId);
        }

        public async Task SaveTimelineJsonAsync(int storyId, int userId, string json, DateTime lastModified)
        {
            if (await IsGoogleUserAsync(userId))
                await _timelineSheetService.SaveTimelineJsonAsync(storyId.ToString(), userId.ToString(), json, lastModified);
            else
                await _storyDataService.SaveTimelineJsonAsync(storyId, json);
        }

        public async Task<JsonWithModifiedDto?> GetTimelineWithLastModifiedAsync(int storyId, int userId)
        {
            if (await IsGoogleUserAsync(userId))
            {
                return await _timelineSheetService.GetTimelineWithLastModifiedAsync(
                    storyId.ToString(), userId.ToString());
            }
            else
            {
                var json = await _storyDataService.GetTimelineJsonAsync(storyId);
                return json == null ? null : new JsonWithModifiedDto
                {
                    Json = json,
                    LastModifiedRaw = DateTime.MinValue.ToString("o")
                };
            }
        }
    }
}