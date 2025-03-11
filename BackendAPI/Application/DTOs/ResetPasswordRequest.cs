using System.ComponentModel.DataAnnotations;

namespace BackendAPI.Application.DTOs
{
    public class ResetPasswordRequest
    {
        [Required]
        public string Token { get; set; } = string.Empty; // 密碼重設 Token

        [Required]
        [MinLength(8)]
        public string NewPassword { get; set; } = string.Empty; // 新密碼
    }
}
