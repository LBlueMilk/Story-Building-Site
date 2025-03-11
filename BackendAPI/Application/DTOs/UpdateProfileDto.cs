namespace BackendAPI.Application.DTOs
{
    public class UpdateProfileDto
    {
        public string? Email { get; set; } // 可選，允許使用者更新 Email
        public string? Name { get; set; }  // 可選，允許使用者更新 Name
    }
}
