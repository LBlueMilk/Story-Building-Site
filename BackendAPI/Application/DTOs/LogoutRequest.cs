namespace BackendAPI.Application.DTOs
{
    public class LogoutRequest
    {
        public string RefreshToken { get; set; } = string.Empty;
        public bool LogoutAllDevices { get; set; } = false;
    }
}
