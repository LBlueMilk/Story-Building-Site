using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// ���X JWT Secret Key�]����@����N Secret �s�J appsettings.json�A�A���ѱ��o��^
//var secretKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
//Console.WriteLine($"Generated JWT Secret: {secretKey}");

// Ū�� JWT Secret Key
var jwtSecret = builder.Configuration["Jwt:Secret"] ?? throw new InvalidOperationException("JWT Secret is missing");

// �]�w JWT ����
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            ValidateIssuer = false, // �ثe�O `false`�A��ܤ��ˬd Token �Ӧۭ��̡A���ݫ�ק�
            //ValidIssuer = "���}", // �]�w�o��̦W��
            ValidateAudience = false, // �ثe�O `false`�A��ܤ��ˬd Token �O���֪��A���ݫ�ק�         
            //ValidAudience = "", // �]�w�����̦W��
            ValidateLifetime = false // �ˬd Token �O�_�L��
        };
    });

builder.Services.AddAuthorization(); // �[�J��������

// �[�J API �A��
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// �]�w HTTP �ШD�B�z�y�{
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication(); // **�@�w�n�b Authorization ���e����**
app.UseAuthorization();

app.MapControllers();

app.Run();
