using PaymentService.Kafka.Consumers;
using PaymentService.Kafka.Producers;
using Microsoft.EntityFrameworkCore;
using PaymentService.Data;


var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<PaymentProducer>();
builder.Services.AddHostedService<OrderCreatedConsumer>();
builder.Services.AddDbContext<PaymentDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("PaymentDb")));


var app = builder.Build();

app.MapGet("/health", () => "PaymentService running");

app.Run();
