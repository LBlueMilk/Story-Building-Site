using System.ComponentModel.DataAnnotations;

namespace BackendAPI.Application.DTOs
{
    public class ToggleProviderDto
    {
        [Required]
        public string Provider { get; set; } = string.Empty;
    }
}
