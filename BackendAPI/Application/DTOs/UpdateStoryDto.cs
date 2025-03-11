namespace BackendAPI.Application.DTOs
{
    public class UpdateStoryDto
    {
        public string? Title { get; set; }
        public string? Description { get; set; }
        public bool? IsPublic { get; set; } 
    }

}
