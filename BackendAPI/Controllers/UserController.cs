using BackendAPI.Models;
using BackendAPI.Services.User;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BackendAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {
        private readonly UserMigrationService _migrationService;

        public UserController(UserMigrationService migrationService)
        {
            _migrationService = migrationService;
        }

        [Authorize]
        [HttpPost("migrate-to-google")]
        public async Task<IActionResult> MigrateToGoogle()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out int userId))
                return Unauthorized("無效的使用者 Token");

            var success = await _migrationService.MigrateAllStoriesToGoogleAsync(userId);

            return success ? Ok("資料搬移完成") : StatusCode(500, "搬移失敗");
        }
    }
}
