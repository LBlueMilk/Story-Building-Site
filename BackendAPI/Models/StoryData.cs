using System.ComponentModel.DataAnnotations.Schema;

namespace BackendAPI.Models
{
    [Table("story_data")]
    public class StoryData
    {        
        public int Id { get; set; }

        [Column("story_id")]
        public int StoryId { get; set; }

        [Column("canvas_json")]
        public string? CanvasJson { get; set; }

        [Column("character_json")]
        public string? CharacterJson { get; set; }

        [Column("timeline_json")]
        public string? TimelineJson { get; set; }

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; }

        public Story? Story { get; set; }  
    }
}
