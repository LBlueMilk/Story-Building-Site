namespace BackendAPI.Models
{
    public class StoryData
    {
        public int Id { get; set; }

        public int StoryId { get; set; }

        public string? CanvasJson { get; set; }

        public string? CharacterJson { get; set; }

        public string? TimelineJson { get; set; }

        public DateTime UpdatedAt { get; set; }

        public Story? Story { get; set; }  
    }
}
