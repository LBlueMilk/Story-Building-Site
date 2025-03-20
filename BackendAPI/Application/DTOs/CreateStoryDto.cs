using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace BackendAPI.Application.DTOs
{
    public class CreateStoryDto
    {
        [Required(ErrorMessage = "標題不能為空")]
        [MaxLength(255, ErrorMessage = "標題長度不能超過 255 個字")]
        public string Title { get; set; } = string.Empty;

        [MaxLength(5000, ErrorMessage = "描述長度不能超過 5000 個字")]
        public string? Description { get; set; }

        [DefaultValue(false)]
        public bool? IsPublic { get; set; } = false;  // 預設值為 false
    }
}
