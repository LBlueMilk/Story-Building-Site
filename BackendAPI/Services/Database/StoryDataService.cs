using BackendAPI.Data;
using BackendAPI.Models;
using Microsoft.EntityFrameworkCore;


namespace BackendAPI.Services.Database
{
    public class StoryDataService : IStoryDataService
    {
        private readonly UserDbContext _db;

        public StoryDataService(UserDbContext db)
        {
            _db = db;
        }

        public async Task<string?> GetCanvasJsonAsync(int storyId)
        {
            return await _db.StoryData
                .Where(x => x.StoryId == storyId)
                .Select(x => x.CanvasJson)
                .FirstOrDefaultAsync();
        }

        public async Task SaveCanvasJsonAsync(int storyId, string json)
        {
            var record = await _db.StoryData.FirstOrDefaultAsync(x => x.StoryId == storyId);
            if (record != null)
            {
                record.CanvasJson = json;
                record.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                _db.StoryData.Add(new StoryData
                {
                    StoryId = storyId,
                    CanvasJson = json,
                    UpdatedAt = DateTime.UtcNow
                });
            }

            await _db.SaveChangesAsync();
        }

        public async Task<string?> GetCharacterJsonAsync(int storyId)
        {
            return await _db.StoryData
                .Where(x => x.StoryId == storyId)
                .Select(x => x.CharacterJson)
                .FirstOrDefaultAsync();
        }

        public async Task SaveCharacterJsonAsync(int storyId, string json)
        {
            var record = await _db.StoryData.FirstOrDefaultAsync(x => x.StoryId == storyId);
            if (record != null)
            {
                record.CharacterJson = json;
                record.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                _db.StoryData.Add(new StoryData
                {
                    StoryId = storyId,
                    CharacterJson = json,
                    UpdatedAt = DateTime.UtcNow
                });
            }

            await _db.SaveChangesAsync();
        }

        public async Task<string?> GetTimelineJsonAsync(int storyId)
        {
            return await _db.StoryData
                .Where(x => x.StoryId == storyId)
                .Select(x => x.TimelineJson)
                .FirstOrDefaultAsync();
        }

        public async Task SaveTimelineJsonAsync(int storyId, string json)
        {
            var record = await _db.StoryData.FirstOrDefaultAsync(x => x.StoryId == storyId);
            if (record != null)
            {
                record.TimelineJson = json;
                record.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                _db.StoryData.Add(new StoryData
                {
                    StoryId = storyId,
                    TimelineJson = json,
                    UpdatedAt = DateTime.UtcNow
                });
            }

            await _db.SaveChangesAsync();
        }
    }
}
