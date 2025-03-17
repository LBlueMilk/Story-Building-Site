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

var builder = WebApplication.CreateBuilder(args);

// 產出 JWT Secret Key（執行一次後將 Secret 存入 appsettings.json，再註解掉這行）
//var secretKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
//Console.WriteLine($"Generated JWT Secret: {secretKey}");

// Extensions資料夾下的ServiceExtensions.cs檔案
builder.Services.ConfigureServices(builder.Configuration);
// Extensions資料夾下的SwaggerExtensions.cs檔案
builder.Services.ConfigureSwagger();

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
}


app.UseCors("AllowLocalhost3000");



app.UseAuthentication(); // **一定要在 Authorization 之前執行**
app.UseHttpsRedirection();
app.UseAuthorization();

app.MapControllers();
app.Run();
