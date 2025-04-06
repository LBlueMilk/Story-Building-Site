using BackendAPI.Services.Storage;

namespace BackendAPI.Services.User
{
    public class UserMigrationService
    {
        private readonly IStorageService _storageService;
        private readonly IStoryDataService _storyDataService;
        private readonly IUserService _userService;

        public UserMigrationService(
            IStorageService storageService,
            IStoryDataService storyDataService,
            IUserService userService)
        {
            _storageService = storageService;
            _storyDataService = storyDataService;
            _userService = userService;
        }

        public async Task<bool> MigrateAllStoriesToGoogleAsync(int userId)
        {
            var stories = await _userService.GetUserStoriesAsync(userId);

            foreach (var story in stories)
            {
                var storyId = story.Id;

                var canvas = await _storyDataService.GetCanvasJsonAsync(storyId);
                var character = await _storyDataService.GetCharacterJsonAsync(storyId);
                var timeline = await _storyDataService.GetTimelineJsonAsync(storyId);

                if (canvas != null)
                    await _storageService.SaveCanvasJsonAsync(storyId, userId, canvas);
                if (character != null)
                    await _storageService.SaveCharacterJsonAsync(storyId, userId, character);
                if (timeline != null)
                    await _storageService.SaveTimelineJsonAsync(storyId, userId, timeline);
            }

            return true;
        }
    }
}
