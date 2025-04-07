namespace BackendAPI.Application.DTOs
{
    public class JsonWithModifiedDto
    {
        public string Json { get; set; } = string.Empty;
        public string? LastModifiedRaw { get; set; }
    }
}
