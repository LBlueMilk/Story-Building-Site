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

        public async Task SaveCanvasJsonAsync(int storyId, int userId, string json)
        {
            if (await IsGoogleUserAsync(userId))
                await _canvasSheetService.SaveCanvasJsonAsync(storyId.ToString(), userId.ToString(), json);
            else
                await _storyDataService.SaveCanvasJsonAsync(storyId, json);
        }

        // ---------- Character ----------
        public async Task<string?> GetCharacterJsonAsync(int storyId, int userId)
        {
            if (await IsGoogleUserAsync(userId))
                return await _characterSheetService.GetCharacterJsonAsync(storyId.ToString(), userId.ToString());
            else
                return await _storyDataService.GetCharacterJsonAsync(storyId);
        }

        public async Task SaveCharacterJsonAsync(int storyId, int userId, string json)
        {
            if (await IsGoogleUserAsync(userId))
                await _characterSheetService.SaveCharacterJsonAsync(storyId.ToString(), userId.ToString(), json);
            else
                await _storyDataService.SaveCharacterJsonAsync(storyId, json);
        }

        // ---------- Timeline ----------
        public async Task<string?> GetTimelineJsonAsync(int storyId, int userId)
        {
            if (await IsGoogleUserAsync(userId))
                return await _timelineSheetService.GetTimelineJsonAsync(storyId.ToString(), userId.ToString());
            else
                return await _storyDataService.GetTimelineJsonAsync(storyId);
        }

        public async Task SaveTimelineJsonAsync(int storyId, int userId, string json)
        {
            if (await IsGoogleUserAsync(userId))
                await _timelineSheetService.SaveTimelineJsonAsync(storyId.ToString(), userId.ToString(), json);
            else
                await _storyDataService.SaveTimelineJsonAsync(storyId, json);
        }
    }
}