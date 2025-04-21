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

// ���X JWT Secret Key�]����@����N Secret �s�J appsettings.json�A�A���ѱ��o��^
//var secretKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
//Console.WriteLine($"Generated JWT Secret: {secretKey}");

// Extensions��Ƨ��U��ServiceExtensions.cs�ɮ�
builder.Services.ConfigureServices(builder.Configuration);
// Extensions��Ƨ��U��SwaggerExtensions.cs�ɮ�
builder.Services.ConfigureSwagger();
// �����M�E�_ EF Core ���઺���~
builder.Services.AddDatabaseDeveloperPageExceptionFilter();
// �]�w��x
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

// ���U�ϥΪ̪A�ȡA�ΨӨ��o�ثe�n�J�̸�T�P Provider �����]Google �ε��U�Τ�^
builder.Services.AddScoped<IUserService, UserService>();
// ���U�G�Ƹ�ƪA�ȡA�t�d PostgreSQL ��Ʀs���]�@����U�b���|�ϥγo�ӪA�ȡ^
builder.Services.AddScoped<IStoryDataService, StoryDataService>();
// ���U���z�x�s�A�ȡA�ھڨϥΪ̨����۰ʿ���x�s�� Google Sheets �� PostgreSQL
builder.Services.AddScoped<IStorageService, SmartStorageService>();
// ���U HttpContextAccessor�A����ݪA�ȡ]�p SmartStorageService�^�i�H���o�ثe�� HttpContext�]�Ω�P�_�n�J�̸�T�^
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

// �]�w HTTP �ШD�B�z�y�{
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseDeveloperExceptionPage();
}


app.UseCors("AllowLocalhost3000");


app.UseAuthentication(); // **�@�w�n�b Authorization ���e����**
// app.UseHttpsRedirection();
app.UseAuthorization();

// �]�w���d�ˬd����Render��
app.MapGet("/healthz", () => Results.Ok("Healthy"));

app.MapControllers();
app.Run();
