using PaymentService.Kafka.Consumers;
using PaymentService.Kafka.Producers;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using PaymentService.Data;
using System.Text;


var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<PaymentProducer>();
builder.Services.AddHostedService<OrderCreatedConsumer>();
builder.Services.AddDbContext<PaymentDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("PaymentDb")));


var app = builder.Build();

app.MapGet("/health", () => "PaymentService running");

app.Run();
