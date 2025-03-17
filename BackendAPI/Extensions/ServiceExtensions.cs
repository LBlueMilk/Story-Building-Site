﻿using BackendAPI.Data;
using BackendAPI.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace BackendAPI.Extensions
{
    public static class ServiceExtensions
    {
        public static void ConfigureServices(this IServiceCollection services, IConfiguration configuration)
        {
            // 設定 PostgreSQL 連線
            services.AddDbContext<UserDbContext>(options =>
                options.UseNpgsql(configuration.GetConnectionString("PostgreSQL")));

            // 讀取 JWT Secret Key
            var jwtSecret = configuration["Jwt:Secret"] ?? throw new InvalidOperationException("JWT Secret is missing");

            // 設定 JWT 驗證
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
                        ValidateIssuer = false, // 目前是 `false`，表示不檢查 Token 來自哪裡，雲端後修改
                        //ValidIssuer = "BackendAPI", // 設定發行者名稱
                        ValidateAudience = false, // 目前是 `false`，表示不檢查 Token 是給誰的，雲端後修改         
                        //ValidAudience = "BackendAPIClients", // 設定接收者名稱
                        ValidateLifetime = true, // 檢查 Token 是否過期
                        ClockSkew = TimeSpan.Zero // 取消預設 5 分鐘誤差
                    };
                });

            // 加入 Email 服務
            services.AddScoped<IEmailService, EmailService>();
            // 加入密碼雜湊服務
            services.AddAuthorization();
            // 加入 API 服務
            services.AddControllers();
        }
    }
}
