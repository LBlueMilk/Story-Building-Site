using BackendAPI.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Security.Cryptography;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// ���X JWT Secret Key�]����@����N Secret �s�J appsettings.json�A�A���ѱ��o��^
//var secretKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
//Console.WriteLine($"Generated JWT Secret: {secretKey}");

// �]�w PostgreSQL �s�u
builder.Services.AddDbContext<UserDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Postgres")));

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
            ValidateLifetime = true, // �ˬd Token �O�_�L��
            ClockSkew = TimeSpan.Zero // �����w�] 5 �����~�t
        };
    });

builder.Services.AddAuthorization(); // �[�J��������

// �[�J API �A��
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// �s�W JWT ���Ҥ䴩�� Swagger
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "BackendAPI", Version = "v1" });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "�п�J `�A�� JWT Token`"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});

var app = builder.Build();

// �]�w HTTP �ШD�B�z�y�{
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication(); // **�@�w�n�b Authorization ���e����**
app.UseHttpsRedirection();
app.UseAuthorization();

app.MapControllers();

app.Run();
