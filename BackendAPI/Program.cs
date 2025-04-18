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


var builder = WebApplication.CreateBuilder(args);

// 產出 JWT Secret Key（執行一次後將 Secret 存入 appsettings.json，再註解掉這行）
//var secretKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
//Console.WriteLine($"Generated JWT Secret: {secretKey}");

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
    options.AddPolicy("AllowLocalhost3000",
        builder => builder
            .WithOrigins("http://localhost:3000")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials()
    );
});


var app = builder.Build();

// 設定 HTTP 請求處理流程
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseDeveloperExceptionPage();
}


app.UseCors("AllowLocalhost3000");


app.UseAuthentication(); // **一定要在 Authorization 之前執行**
//app.UseHttpsRedirection();
app.UseAuthorization();

app.MapControllers();
app.Run();
