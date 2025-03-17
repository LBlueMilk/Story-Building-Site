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

// ���X JWT Secret Key�]����@����N Secret �s�J appsettings.json�A�A���ѱ��o��^
//var secretKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
//Console.WriteLine($"Generated JWT Secret: {secretKey}");

// Extensions��Ƨ��U��ServiceExtensions.cs�ɮ�
builder.Services.ConfigureServices(builder.Configuration);
// Extensions��Ƨ��U��SwaggerExtensions.cs�ɮ�
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

// �]�w HTTP �ШD�B�z�y�{
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}


app.UseCors("AllowLocalhost3000");



app.UseAuthentication(); // **�@�w�n�b Authorization ���e����**
app.UseHttpsRedirection();
app.UseAuthorization();

app.MapControllers();
app.Run();
