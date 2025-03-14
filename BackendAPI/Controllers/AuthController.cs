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
using BackendAPI.Services;
using BackendAPI.Application.DTOs;
using System.Text.RegularExpressions;
using BackendAPI.Utils;


namespace BackendAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly UserDbContext _context;
        private readonly IConfiguration _config;
        private readonly IEmailService _emailService;
        private readonly ILogger<AuthController> _logger;

        // 最大失敗登入次數
        private const int MaxFailedAttempts = 5;
        private const int LockoutMinutes = 15;

        public AuthController(UserDbContext context, IConfiguration config, IEmailService emailService, ILogger<AuthController> logger)
        {
            _context = context;
            _config = config;
            _emailService = emailService;
            _logger = logger;
        }

        // 註冊 API
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterUserDto userDto)
        {
            // 檢查 Email 和密碼是否為空
            if (string.IsNullOrWhiteSpace(userDto.Email) || string.IsNullOrWhiteSpace(userDto.Password) || string.IsNullOrWhiteSpace(userDto.Name))
            {
                return BadRequest(new { message = "Email、密碼、名稱不能為空" });
            }

            // 檢查 Email 格式
            if (!EmailValidator.IsValidEmail(userDto.Email))
            {
                return BadRequest(new { message = "Email 格式不正確或為無效網域" });
            }

            // 檢查 Email 格式，去除空格並轉為小寫
            userDto.Email = userDto.Email.Trim().ToLower();            

            // 檢查 Email 是否已經被註冊
            if (await _context.Users.AnyAsync(u => u.Email == userDto.Email))
            {
                return BadRequest(new { message = "Email 已被註冊" });
            }

            // 檢查密碼強度
            if (!IsPasswordValid(userDto.Password))
            {
                return BadRequest(new { message = "密碼至少 8 碼，需包含大小寫字母、數字、特殊符號。" });
            }

            // 產生 Email 驗證 Token（UUID）
            string verificationToken = Guid.NewGuid().ToString();

            // 建立新使用者
            var newUser = new User
            {
                Email = userDto.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(userDto.Password, workFactor: 12),
                Name = userDto.Name,
                CreatedAt = DateTime.UtcNow,
                IsVerified = false,
                UserCode = GenerateUserCode()
                //EmailVerificationToken = verificationToken // 存入驗證 Token 上線才用
            };

            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();

            // 在正式環境應該發送 Email，而不是直接回傳 Token
            //string verificationLink = $"https://你的前端網址/verify-email?token={verificationToken}";
            //await _emailService.SendAsync(
            //    newUser.Email,
            //    "請驗證你的 Email",
            //    $"請點擊以下連結驗證你的 Email：<a href='{verificationLink}'>驗證 Email</a>"
            //);

            return Ok(new { message = "註冊成功，請查收 Email 進行驗證。" , verificationToken });
        }

        // 產生使用者代碼
        private string GenerateUserCode()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var random = new Random();
            string code;

            do
            {
                code = new string(Enumerable.Repeat(chars, 8)
                    .Select(s => s[random.Next(s.Length)]).ToArray());
            } while (_context.Users.Any(u => u.UserCode == code)); // 確保唯一性

            return code;
        }

        // 測試用驗證 Email API
        [HttpGet("verify-email")]
        public async Task<IActionResult> VerifyEmail([FromQuery] string token)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => !u.IsVerified);

            if (user == null)
            {
                return BadRequest(new { message = "無效的測試用驗證 Token，或所有用戶都已驗證" });
            }

            user.IsVerified = true;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Email 測試驗證成功，您現在可以登入" });
        }

        // 正式用驗證 Email API
        //[HttpGet("verify-email")]
        //public async Task<IActionResult> VerifyEmail([FromQuery] string token)
        //{
        //    var user = await _context.Users.FirstOrDefaultAsync(u => u.EmailVerificationToken == token);

        //    if (user == null)
        //    {
        //        return BadRequest(new { message = "無效的驗證 Token。" });
        //    }

        //    user.IsVerified = true;
        //    user.EmailVerificationToken = null; // 清除 Token
        //    await _context.SaveChangesAsync();

        //    return Ok(new { message = "Email 驗證成功，您現在可以登入。" });
        //}


        // 登入 API（產生 Access Token + Refresh Token）
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
        {
            // 檢查 Email 和密碼是否為空
            if (string.IsNullOrWhiteSpace(loginDto.Email) || string.IsNullOrWhiteSpace(loginDto.Password))
            {
                return BadRequest(new { message = "Email 和密碼不能為空" });
            }

            // 將 Email 轉為小寫
            var email = loginDto.Email.ToLower();
            // 取得使用者
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

            // 檢查使用者是否存在
            if (user == null)
            {
                return Unauthorized(new { message = "登入失敗，請檢查您的帳號或密碼" });
            }

            // 檢查帳戶是否已鎖定
            if (user.FailedLoginAttempts >= MaxFailedAttempts &&
                user.LastFailedLogin.HasValue &&
                (DateTime.UtcNow - user.LastFailedLogin.Value).TotalMinutes < LockoutMinutes)
            {
                return Unauthorized(new { message = $"帳戶已鎖定，請稍後再試。" });
            }

            // 檢查密碼是否存在，避免 OAuth 使用者嘗試密碼登入
            if (string.IsNullOrEmpty(user.PasswordHash))
            {
                return Unauthorized(new { message = "此帳戶無法使用密碼登入，請使用 OAuth 或重設密碼" });
            }

            // 驗證密碼是否正確
            if (!BCrypt.Net.BCrypt.Verify(loginDto.Password, user.PasswordHash))
            {
                user.FailedLoginAttempts++;
                user.LastFailedLogin = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                return Unauthorized(new { message = "登入失敗，請檢查您的帳號或密碼" });
            }

            // 登入成功，重置失敗次數，更新最後登入時間
            user.FailedLoginAttempts = 0;
            user.LastFailedLogin = null;
            user.LastLogin = DateTime.UtcNow;

            // 產生新的 Access Token
            var accessToken = GenerateJwtToken(user);

            // 產生新的 Refresh Token
            string refreshToken = Guid.NewGuid().ToString();
            string refreshTokenHash = BCrypt.Net.BCrypt.HashPassword(refreshToken);

            // 清理過期 Refresh Token
            try
            {
                const int batchSize = 1000;
                int deletedRows;

                do
                {
                    deletedRows = await _context.RefreshTokens
                        .Where(rt => rt.ExpiresAt <= DateTime.UtcNow || rt.RevokedAt != null)
                        .Take(batchSize)
                        .ExecuteDeleteAsync();
                } while (deletedRows > 0);
            }
            catch (Exception ex)
            {
                _logger?.LogWarning($"[Login] 刪除過期 Refresh Token 時發生錯誤: {ex.Message}");
            }

            // 設定過期時間
            var refreshTokenExpiration = _config.GetValue<int>("Jwt:RefreshTokenExpiration");

            // 記錄裝置資訊
            var refreshTokenEntity = new RefreshToken
            {
                UserId = user.Id,
                TokenHash = refreshTokenHash,
                DeviceInfo = Request.Headers["User-Agent"].ToString(),
                ExpiresAt = DateTime.UtcNow.AddDays(refreshTokenExpiration), // 使用 appsettings 設定的過期時間
                CreatedAt = DateTime.UtcNow
            };  

            // 儲存新的 Refresh Token
            _context.RefreshTokens.Add(refreshTokenEntity);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                accessToken,
                refreshToken // 讓前端存起來，未來用來換取新的 Access Token
            });
        }

        // 產生 Access Token
        private string GenerateJwtToken(User user)
        {
            var jwtSecret = _config["Jwt:Secret"] ?? throw new InvalidOperationException("JWT Secret is missing");
            var issuer = _config["Jwt:Issuer"] ?? "backendapi";
            var audience = _config["Jwt:Audience"] ?? "frontendapp";

            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(jwtSecret);
            var accessTokenExpiration = _config.GetValue<int>("Jwt:AccessTokenExpiration");

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()), // 使用者 ID
                    new Claim(ClaimTypes.Email, user.Email ?? "") // 使用者 Email
                }),
                Expires = DateTime.UtcNow.AddHours(accessTokenExpiration), // 使用 appsettings 設定的過期時間
                Issuer = issuer,
                Audience = audience,
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        // 使用 Refresh Token 取得新的 Access Token
        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
        {
            if (string.IsNullOrEmpty(request.RefreshToken))
                return BadRequest(new { message = "請提供 Refresh Token" });

            // 先清理過期的 Refresh Token
            try
            {
                const int batchSize = 1000;
                int deletedRows;

                do
                {
                    deletedRows = await _context.RefreshTokens
                        .Where(rt => rt.ExpiresAt <= DateTime.UtcNow || rt.RevokedAt != null)
                        .Take(batchSize)
                        .ExecuteDeleteAsync();
                } while (deletedRows > 0);
            }
            catch (Exception ex)
            {
                _logger?.LogWarning($"[RefreshToken] 刪除過期 Refresh Token 時發生錯誤: {ex.Message}");
            }

            // 先取得該使用者所有有效的 Refresh Token
            var storedToken = await _context.RefreshTokens
                .Include(rt => rt.User)
                .FirstOrDefaultAsync(rt => rt.ExpiresAt > DateTime.UtcNow && rt.RevokedAt == null);

            if (storedToken == null || !BCrypt.Net.BCrypt.Verify(request.RefreshToken, storedToken.TokenHash))
                return Unauthorized(new { message = "Refresh Token 無效或已過期，請重新登入" });

            try
            {
                // 撤銷舊的 Refresh Token
                storedToken.RevokedAt = DateTime.UtcNow;

                // 產生新的 Refresh Token
                string newRefreshToken = Guid.NewGuid().ToString();
                string newRefreshTokenHash = BCrypt.Net.BCrypt.HashPassword(newRefreshToken);
                // 設定過期時間
                var refreshTokenExpiration = _config.GetValue<int>("Jwt:RefreshTokenExpiration");

                var newToken = new RefreshToken
                {
                    UserId = storedToken.UserId,
                    TokenHash = newRefreshTokenHash,
                    DeviceInfo = storedToken.DeviceInfo,
                    ExpiresAt = DateTime.UtcNow.AddDays(refreshTokenExpiration), // 使用 appsettings 設定的過期時間
                    CreatedAt = DateTime.UtcNow
                };

                _context.RefreshTokens.Add(newToken);
                await _context.SaveChangesAsync();

                // 產生新的 Access Token
                var newAccessToken = GenerateJwtToken(storedToken.User);

                return Ok(new { accessToken = newAccessToken, refreshToken = newRefreshToken });
            }
            catch (Exception ex)
            {
                _logger?.LogError($"[RefreshToken] 無法儲存新的 Refresh Token: {ex.Message}");
                return StatusCode(500, new { message = "系統錯誤，請稍後再試" });
            }
        }

        // 登出 API
        [Authorize]
        [HttpPost("logout")]
        public async Task<IActionResult> Logout([FromBody] LogoutRequest request)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null || !int.TryParse(userId, out int parsedUserId))
                return Unauthorized(new { message = "無效的 Token" });

            if (string.IsNullOrEmpty(request.RefreshToken))
                return BadRequest(new { message = "請提供 Refresh Token" });

            // 先取得該使用者所有有效的 Refresh Token
            var storedTokens = await _context.RefreshTokens
                .Where(rt => rt.UserId == parsedUserId && rt.RevokedAt == null)
                .ToListAsync();

            
            var tokenToRevoke = storedTokens.FirstOrDefault(rt => BCrypt.Net.BCrypt.Verify(request.RefreshToken, rt.TokenHash));

            if (tokenToRevoke == null)
                return Unauthorized(new { message = "Refresh Token 無效或已撤銷" });


            if (request.LogoutAllDevices)
            {
                // 撤銷該使用者所有 Refresh Token（登出所有裝置）
                await _context.RefreshTokens
                    .Where(rt => rt.UserId == parsedUserId)
                    .ExecuteDeleteAsync();
            }
            else
            {
                // 只撤銷當前 Refresh Token
                tokenToRevoke.RevokedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }

            return Ok(new { message = request.LogoutAllDevices ? "已登出所有裝置" : "登出成功" });
        }



        // 取得使用者個人資訊（需登入）
        [Authorize] // 需要 JWT Token
        [HttpGet("profile")]
        public async Task<IActionResult> GetProfile()
        {
            // 從 JWT Token 取得使用者 ID
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userId, out int parsedUserId))
                return Unauthorized(new { message = "無效的 Token" });


            // 查找使用者
            var user = await _context.Users.FindAsync(parsedUserId);
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
                Email = user.Email ?? "", // 確保 Email 不是 null
                user.Name,
                user.UserCode,
                user.IsVerified,
                user.CreatedAt,
                loginProviders = providers.Count > 0 ? providers : new List<string> { "email" }
            });
        }

        // 更新使用者個人資料（需登入）
        [Authorize]
        [HttpPut("update")]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto updatedUser)
        {
            // 從 JWT Token 取得使用者 ID
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null || !int.TryParse(userId, out int parsedUserId))
                return Unauthorized(new { message = "無效的 Token（找不到使用者 ID）" });

            // 查找使用者
            var user = await _context.Users.FindAsync(parsedUserId);
            if (user == null)
                return NotFound(new { message = "找不到使用者" });

            // 記錄是否有變更
            bool hasChanges = false;

            // 檢查 Email 是否有效
            if (!string.IsNullOrWhiteSpace(updatedUser.Email)) // 允許不變更 Email
            {
                updatedUser.Email = updatedUser.Email.Trim().ToLower(); // 轉小寫 + 去除前後空格

                // 檢查 Email 是否為空
                if (string.IsNullOrEmpty(updatedUser.Email))
                {
                    return BadRequest(new { message = "Email 不能為空" });
                }

                // 檢查 Email 格式
                if (!EmailValidator.IsValidEmail(updatedUser.Email))
                {
                    return BadRequest(new { message = "Email 格式不正確或為無效網域" });
                }

                // Email 變更時，檢查是否已被其他帳號使用
                if (updatedUser.Email != user.Email)
                {
                    bool emailExists = await _context.Users.AnyAsync(u => u.Email == updatedUser.Email && u.Id != user.Id);
                    if (emailExists)
                    {
                        return BadRequest(new { message = "該 Email 已被其他帳號使用" });
                    }

                    // **更新 Email，並標記為未驗證**
                    user.Email = updatedUser.Email;
                    user.IsVerified = false; // 變更 Email 後，要求重新驗證
                    user.EmailVerificationToken = Guid.NewGuid().ToString(); // 產生新的驗證 Token
                    hasChanges = true;

                    // 📧 這裡應該發送驗證信（目前先留空）
                    // await _emailService.SendVerificationEmail(user.Email, user.EmailVerificationToken);
                }
            }

            // 檢查 Name 是否有效
            if (!string.IsNullOrWhiteSpace(updatedUser.Name) && updatedUser.Name != user.Name)
            {
                user.Name = updatedUser.Name;
                hasChanges = true;
            }

            if (hasChanges)
            {
                _context.Users.Update(user);
                await _context.SaveChangesAsync();
                return Ok(new { message = "個人資料更新成功，若變更 Email，請前往驗證" });
            }

            return Ok(new { message = "沒有變更，無需更新" });
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
                    ClockSkew = TimeSpan.FromMinutes(2) // 允許 2 分鐘誤差，防止 Token 驗證問題
                };

                var principal = tokenHandler.ValidateToken(request.Token, tokenValidationParameters, out _);
                var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                // 檢查使用者 ID 是否存在
                if (userIdClaim == null || !int.TryParse(userIdClaim, out int parsedUserId))
                    return Unauthorized(new { message = "無效的 Token" });

                // 查找使用者
                var user = await _context.Users.FindAsync(parsedUserId);
                if (user == null)
                    return NotFound(new { message = "找不到使用者" });

                // 檢查此 Token 是否已經被使用
                if (await _context.ResetPasswordTokens.AnyAsync(t => t.UserId == user.Id))                
                    return Unauthorized(new { message = "此密碼重設連結已經被使用，請重新申請。" });                

                // 檢查密碼強度
                if (!IsPasswordValid(request.NewPassword))
                    return BadRequest(new { message = "密碼至少 8 碼，且需包含大寫、小寫、數字與特殊符號。" });

                // 檢查新密碼是否與舊密碼相同
                if (BCrypt.Net.BCrypt.Verify(request.NewPassword, user.PasswordHash))
                    return BadRequest(new { message = "新密碼不能與舊密碼相同" });                

                // 使用 Transaction 確保一致性
                await using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    // 更新密碼（加密，使用 work factor = 12）
                    user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword, workFactor: 12);
                    user.LastPasswordChange = DateTime.UtcNow;

                    // 讓 Refresh Token 失效（最佳化刪除）
                    try
                    {
                        await _context.RefreshTokens
                                      .Where(rt => rt.UserId == user.Id)
                                      .ExecuteDeleteAsync(); // 直接刪除，提高效能
                    }
                    catch (Exception ex)
                    {
                        if (_logger != null)
                            _logger.LogWarning($"[ResetPassword] `ExecuteDeleteAsync` 失敗，改用 RemoveRange: {ex.Message}");

                        var refreshTokens = await _context.RefreshTokens
                                                          .Where(rt => rt.UserId == user.Id)
                                                          .ToListAsync();
                        _context.RefreshTokens.RemoveRange(refreshTokens);
                        await _context.SaveChangesAsync(); // 確保刪除生效
                    }

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    return Ok(new { message = "密碼重設成功，請重新登入" });
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync(); // 確保發生錯誤時回滾
                    if (_logger != null)
                        _logger.LogError($"[ResetPassword] 密碼更新失敗: {ex.Message}\n{ex.StackTrace}");

                    return StatusCode(500, new { message = "伺服器錯誤，請稍後再試" });
                }
            }
            catch (SecurityTokenExpiredException)
            {
                return Unauthorized(new { message = "密碼重設連結已過期，請重新申請。" });
            }
            catch (SecurityTokenException)
            {
                return Unauthorized(new { message = "無效的 Token，請重新申請密碼重設。" });
            }
        }

        // 檢查密碼強度
        private bool IsPasswordValid(string password)
        {
            // 密碼至少 8 碼，且需包含大寫、小寫、數字與特殊符號
            var regex = new Regex(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[!@#$%^&*()_+{}\[\]:;""'<>,.?/~`\\|-])[A-Za-z\d!@#$%^&*()_+{}\[\]:;""'<>,.?/~`\\|-]{8,}$");

            return regex.IsMatch(password);
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

            // 產生重設密碼 Token（有效期限預設 30 分鐘）
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

        // 刪除帳號 API
        [Authorize]
        [HttpDelete("delete-account")]
        public async Task<IActionResult> DeleteAccount()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
                return Unauthorized(new { message = "無效的 Token" });

            var user = await _context.Users.FindAsync(int.Parse(userId));
            if (user == null)
                return NotFound(new { message = "找不到使用者" });

            if (user.DeletedAt.HasValue)
                return BadRequest(new { message = "帳號已刪除，無法重複刪除" });

            // 設定 deleted_at
            user.DeletedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return Ok(new { message = "帳號刪除成功，30 天內可恢復" });
        }

        // 還原帳號 API
        [Authorize]
        [HttpPut("restore-account")]
        public async Task<IActionResult> RestoreAccount()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
                return Unauthorized(new { message = "無效的 Token" });

            var user = await _context.Users.FindAsync(int.Parse(userId));
            if (user == null)
                return NotFound(new { message = "找不到使用者" });

            if (!user.DeletedAt.HasValue)
                return BadRequest(new { message = "帳號未刪除，無需還原" });

            // 清除 deleted_at
            user.DeletedAt = null; 
            user.RestoredAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return Ok(new { message = "帳號已成功還原" });
        }

        // 清除過期帳號 API
        [HttpPost("cleanup")]
        public async Task<IActionResult> CleanupInactiveAccounts()
        {
            int cleanupDays = Environment.GetEnvironmentVariable("CLEANUP_THRESHOLD_DAYS") is string envValue && int.TryParse(envValue, out int days)
                ? days
                : 30;

            var thresholdDate = DateTime.UtcNow.AddDays(-cleanupDays);

            var expiredUsers = await _context.Users
                .Where(u => u.DeletedAt.HasValue && u.DeletedAt.Value < thresholdDate && !u.RestoredAt.HasValue)
                .ToListAsync();

            if (!expiredUsers.Any())
            {
                return Ok(new { message = "沒有過期帳戶需要刪除。" });
            }

            _context.Users.RemoveRange(expiredUsers);
            await _context.SaveChangesAsync();

            // 在正式環境應該發送 Email 通知管理員
            //await _emailService.SendAsync("之後用自己的信箱", "帳號清理通知", $"已刪除 {expiredUsers.Count} 個過期帳號。");

            return Ok(new { message = $"{expiredUsers.Count} 個過期帳戶已被刪除。" });
        }

        // 連結與斷開 OAuth 帳號 API
        [Authorize]
        [HttpPost("toggle-provider")]
        public async Task<IActionResult> ToggleProvider([FromBody] ToggleProviderDto request)
        {
            // 取得當前用戶 ID
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null || !int.TryParse(userId, out int parsedUserId))
                return Unauthorized(new { message = "無效的 Token（找不到使用者 ID）" });

            // 查詢對應的 OAuth 記錄
            var record = await _context.UserProviders
                .FirstOrDefaultAsync(up => up.UserId == parsedUserId && up.Provider == request.Provider);

            if (record == null)
            {
                return NotFound(new { message = "未找到對應的登入方式" });
            }

            // 切換 `disconnected_at` 狀態
            if (record.DisconnectedAt == null)
            {
                // OAuth 授權撤銷
                if (!string.IsNullOrEmpty(record.ProviderId))
                {
                    try
                    {
                        await RevokeOAuthToken(record.Provider, record.ProviderId);
                    }
                    catch (Exception ex)
                    {
                        return StatusCode(500, new { message = $"取消 {record.Provider} 授權失敗: {ex.Message}" });
                    }
                }

                // 設為已斷開
                record.DisconnectedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                return Ok(new { message = $"{record.Provider} 已斷開連結" });
            }
            else
            {
                // 重新連結（清除斷開時間）
                record.DisconnectedAt = null;
                await _context.SaveChangesAsync();
                return Ok(new { message = $"{record.Provider} 已重新連結" });
            }
        }

        // 通知 OAuth 取消授權
        private async Task RevokeOAuthToken(string provider, string providerId)
        {
            string revokeUrl = provider switch
            {
                "google" => $"https://oauth2.googleapis.com/revoke?token={providerId}",
                "facebook" => $"https://graph.facebook.com/me/permissions?access_token={providerId}",
                _ => throw new ArgumentException("不支援的 OAuth 供應商")
            };

            using HttpClient client = new();
            var response = await client.GetAsync(revokeUrl);

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"無法取消 {provider} 授權，錯誤代碼: {response.StatusCode}");
            }
        }

    }
}
