using BackendAPI.Data;
using BackendAPI.Extensions;
using BackendAPI.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Security.Cryptography;
using System.Text;
using static BackendAPI.Controllers.AuthController;
using Microsoft.AspNetCore.Diagnostics.EntityFrameworkCore;
using BackendAPI.Services.Storage;
using BackendAPI.Services.Database;
using BackendAPI.Services.User;
using Microsoft.AspNetCore.Mvc;


var builder = WebApplication.CreateBuilder(args);

// 產出 JWT Secret Key（執行一次後將 Secret 存入 appsettings.json，再註解掉這行）
//var secretKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
//Console.WriteLine($"Generated JWT Secret: {secretKey}");

// 錯誤資料回傳格式
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.SuppressMapClientErrors = false;
});


// Extensions資料夾下的ServiceExtensions.cs檔案
builder.Services.ConfigureServices(builder.Configuration);
// Extensions資料夾下的SwaggerExtensions.cs檔案
builder.Services.ConfigureSwagger();
// 偵測和診斷 EF Core 移轉的錯誤
builder.Services.AddDatabaseDeveloperPageExceptionFilter();
// 設定日誌
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

// 註冊使用者服務，用來取得目前登入者資訊與 Provider 類型（Google 或註冊用戶）
builder.Services.AddScoped<IUserService, UserService>();
// 註冊故事資料服務，負責 PostgreSQL 資料存取（一般註冊帳號會使用這個服務）
builder.Services.AddScoped<IStoryDataService, StoryDataService>();
// 註冊智慧儲存服務，根據使用者身分自動選擇儲存到 Google Sheets 或 PostgreSQL
builder.Services.AddScoped<IStorageService, SmartStorageService>();
// 註冊 HttpContextAccessor，讓後端服務（如 SmartStorageService）可以取得目前的 HttpContext（用於判斷登入者資訊）
builder.Services.AddHttpContextAccessor();


builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(
            "http://localhost:3000", // 本機開發
            "https://story-building-site-fe.vercel.app" // 雲端開發
        )
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials(); // 若你使用 cookie 或 JWT 附在 Header 時需要
    });
});


// 本機開發
var app = builder.Build();

// 開發工具
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseDeveloperExceptionPage();
}

// 路由
app.UseRouting();
// 本機開發
//app.UseCors("AllowLocalhost3000");
// 雲端開發
app.UseCors("AllowFrontend");

// 明確允許處理 OPTIONS 請求（預檢請求）
// 如果你有中間件阻擋 OPTIONS，CORS 不會成功
app.Use(async (context, next) =>
{
    if (context.Request.Method == "OPTIONS")
    {
        context.Response.StatusCode = 200;
        await context.Response.CompleteAsync();
        return;
    }
    await next();
});

app.UseAuthentication(); // **一定要在 Authorization 之前執行**
// app.UseHttpsRedirection();
app.UseAuthorization();

// 設定健康檢查路由Render用
app.MapGet("/healthz", () => Results.Ok("Healthy"));
// 自動執行資料庫創建和遷移
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<UserDbContext>();
    db.Database.Migrate();
}

app.MapControllers();
app.Run();
