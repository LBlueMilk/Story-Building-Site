using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// 產出 JWT Secret Key（執行一次後將 Secret 存入 appsettings.json，再註解掉這行）
//var secretKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
//Console.WriteLine($"Generated JWT Secret: {secretKey}");

// 讀取 JWT Secret Key
var jwtSecret = builder.Configuration["Jwt:Secret"] ?? throw new InvalidOperationException("JWT Secret is missing");

// 設定 JWT 驗證
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            ValidateIssuer = false, // 目前是 `false`，表示不檢查 Token 來自哪裡，雲端後修改
            //ValidIssuer = "網址", // 設定發行者名稱
            ValidateAudience = false, // 目前是 `false`，表示不檢查 Token 是給誰的，雲端後修改         
            //ValidAudience = "", // 設定接收者名稱
            ValidateLifetime = false // 檢查 Token 是否過期
        };
    });

builder.Services.AddAuthorization(); // 加入身份驗證

// 加入 API 服務
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// 設定 HTTP 請求處理流程
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication(); // **一定要在 Authorization 之前執行**
app.UseAuthorization();

app.MapControllers();

app.Run();
