using BackendAPI.Application.DTOs;
using BackendAPI.Services;
using BackendAPI.Utils;
using Microsoft.AspNetCore.Mvc;

namespace BackendAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ContactController : ControllerBase
    {
        private readonly IEmailService _emailService;
        private readonly IConfiguration _config;
        private readonly string _receiverEmail;

        public ContactController(IEmailService emailService, IConfiguration config)
        {
            _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _receiverEmail = _config["Email:ReceiverEmail"] ?? throw new ArgumentNullException("ReceiverEmail not configured");
        }

        [HttpPost]
        [Route("send")]
        public async Task<IActionResult> SendContactEmail([FromBody] ContactRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (!EmailValidator.IsValidEmail(request.Email))
            {
                return BadRequest(new { error = "Email 格式錯誤，或不屬於允許的網域。" });
            }

            var subject = $"來自 {request.Name} 的聯絡訊息";
            var body = $"姓名: {request.Name}\nEmail: {request.Email}\n\n訊息內容:\n{request.Message}";

            await _emailService.SendAsync(_receiverEmail, subject, body);

            return Ok(new { message = "Email 已發送" });
        }
    }
}
