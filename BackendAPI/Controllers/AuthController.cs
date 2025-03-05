using Microsoft.EntityFrameworkCore;
using BackendAPI.Models;
using BackendAPI.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;

namespace BackendAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly UserDbContext _context;
        private readonly IConfiguration _config;

        public AuthController(UserDbContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }

        // 註冊 API
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] User user)
        {
            // 檢查 Email 和密碼是否為空
            if (string.IsNullOrEmpty(user.Email) || string.IsNullOrEmpty(user.PasswordHash))
            {
                return BadRequest(new { message = "Email 和密碼不能為空" });
            }

            // 檢查 Email 是否已經被註冊
            if (await _context.Users.AnyAsync(u => u.Email == user.Email))
            {
                return BadRequest(new { message = "Email 已被註冊" });
            }

            // 確保 Id 為 0，讓資料庫自動產生
            user.Id = 0;

            // 將密碼加密存入資料庫
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(user.PasswordHash);
            user.CreatedAt = DateTime.UtcNow;// 確保為 UTC

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return Ok(new { message = "註冊成功" });
        }

        // 登入 API
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] User loginUser)
        {
            // 檢查是否存在該 Email 的使用者
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == loginUser.Email);

            // 驗證密碼是否正確
            if (user == null || !BCrypt.Net.BCrypt.Verify(loginUser.PasswordHash, user.PasswordHash))
            {
                return Unauthorized(new { message = "Email 或密碼錯誤" });
            }

            // 產生 JWT Token
            var token = GenerateJwtToken(user);
            return Ok(new { token });
        }

        // 產生 JWT Token
        private string GenerateJwtToken(User user)
        {
            var jwtSecret = _config["Jwt:Secret"] ?? throw new InvalidOperationException("JWT Secret is missing");

            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(jwtSecret);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()), // 使用者 ID
                    new Claim(ClaimTypes.Email, user.Email ?? "") // 使用者 Email
                }),
                Expires = DateTime.UtcNow.AddHours(12), // 設定 Token 12 小時後過期
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        // 取得使用者個人資訊（需登入）
        [Authorize] // 需要 JWT Token
        [HttpGet("profile")]
        public async Task<IActionResult> GetProfile()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
                return Unauthorized(new { message = "無效的 Token" });

            var user = await _context.Users.FindAsync(int.Parse(userId));
            if (user == null)
                return NotFound(new { message = "找不到使用者" });

            // 查詢該使用者所有綁定的登入方式
            var providers = await _context.UserProviders
                                          .Where(up => up.UserId == user.Id)
                                          .Select(up => up.Provider)
                                          .ToListAsync();

            return Ok(new
            {
                user.Id,
                user.Email,
                user.Name,
                user.IsVerified,
                user.CreatedAt,
                loginProviders = providers.Count > 0 ? providers : new List<string> { "email" }
            });
        }


        // 更新使用者個人資料（需登入）
        [Authorize]
        [HttpPut("update")]
        public async Task<IActionResult> UpdateProfile([FromBody] User updatedUser)
        {
            // 從 JWT Token 取得使用者 ID
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (userId == null)
                return Unauthorized(new { message = "無效的 Token（找不到使用者 ID）" });

            // 查找使用者
            var user = await _context.Users.FindAsync(int.Parse(userId));

            if (user == null)
            {
                return NotFound(new { message = "找不到使用者" });
            }

            // 只能修改 `Email` 和 `Name`
            user.Email = updatedUser.Email ?? user.Email;
            user.Name = updatedUser.Name ?? user.Name;

            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            return Ok(new { message = "個人資料更新成功" });
        }

        // 重設密碼 API
        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
        {
            var jwtSecret = _config["Jwt:Secret"] ?? throw new InvalidOperationException("JWT Secret is missing");
            var tokenHandler = new JwtSecurityTokenHandler();

            try
            {
                var key = Encoding.UTF8.GetBytes(jwtSecret);
                var tokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero // 取消 5 分鐘誤差
                };

                var principal = tokenHandler.ValidateToken(request.Token, tokenValidationParameters, out _);
                var userId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (userId == null)
                    return Unauthorized(new { message = "無效的 Token" });

                // 查找使用者
                var user = await _context.Users.FindAsync(int.Parse(userId));
                if (user == null)
                    return NotFound(new { message = "找不到使用者" });

                // 檢查新密碼是否與舊密碼相同
                if (BCrypt.Net.BCrypt.Verify(request.NewPassword, user.PasswordHash))
                {
                    return BadRequest(new { message = "新密碼不能與舊密碼相同" });
                }

                // 更新密碼（加密）
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
                await _context.SaveChangesAsync();

                return Ok(new { message = "密碼重設成功，請重新登入" });
            }
            catch (SecurityTokenException)
            {
                return Unauthorized(new { message = "無效或過期的 Token" });
            }
        }

        // 更改密碼 API
        [Authorize]
        [HttpPut("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            // 取得當前登入的使用者 ID
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
                return Unauthorized(new { message = "無效的 Token（找不到使用者 ID）" });

            // 查找使用者
            var user = await _context.Users.FindAsync(int.Parse(userId));
            if (user == null)
                return NotFound(new { message = "找不到使用者" });

            // 驗證舊密碼是否正確
            if (!BCrypt.Net.BCrypt.Verify(request.OldPassword, user.PasswordHash))
                return BadRequest(new { message = "舊密碼不正確" });

            // 更新新密碼（雜湊加密後存入）
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);

            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            return Ok(new { message = "密碼變更成功" });
        }

        // 忘記密碼 API
        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
        {
            // 檢查 email 是否為空
            if (string.IsNullOrEmpty(request.Email))
                return BadRequest(new { message = "請提供 Email" });

            // 查找使用者
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
            if (user == null)
                return NotFound(new { message = "此 Email 尚未註冊" });

            // 產生重設密碼 Token（有效期限 30 分鐘）
            var token = GenerateResetPasswordToken(user);

            // 在正式環境應該發送 Email，而不是直接回傳 token
            Console.WriteLine($"產生的重設密碼 Token: {token}");

            return Ok(new { message = "重設密碼信已發送，請檢查您的信箱", token }); // 測試用，正式環境不回傳 Token
        }

        // 產生重設密碼 Token
        private string GenerateResetPasswordToken(User user)
        {
            var jwtSecret = _config["Jwt:Secret"] ?? throw new InvalidOperationException("JWT Secret is missing");

            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(jwtSecret);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email ?? "")
        }),
                Expires = DateTime.UtcNow.AddMinutes(30), // Token 30 分鐘後過期
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }


    }
}
