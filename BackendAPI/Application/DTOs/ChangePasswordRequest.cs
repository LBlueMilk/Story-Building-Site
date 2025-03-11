namespace BackendAPI.Application.DTOs
{
    public class ChangePasswordRequest
    {
        public string OldPassword { get; set; } = string.Empty; // 舊密碼
        public string NewPassword { get; set; } = string.Empty; // 新密碼
    }
}
