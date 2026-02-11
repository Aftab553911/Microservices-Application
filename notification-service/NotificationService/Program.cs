using Microsoft.EntityFrameworkCore;
using NotificationService.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using NotificationService.Kafka.Producers;
using NotificationService.Kafka.Consumers;
using System.IdentityModel.Tokens.Jwt;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<NotificationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("NotificationDb")));
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["JwtSettings:Issuer"],
            ValidAudience = builder.Configuration["JwtSettings:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(
                    builder.Configuration["JwtSettings:SecretKey"]!
                ))
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddSingleton<NotificationProducer>();
builder.Services.AddMemoryCache();
builder.Services.AddHostedService<PaymentResultConsumer>();

var app = builder.Build();
app.UseAuthentication();
app.UseAuthorization();


app.UseHttpsRedirection();

app.Run();
