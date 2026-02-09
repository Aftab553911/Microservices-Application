using Microsoft.EntityFrameworkCore;
using NotificationService.Data;
using NotificationService.Kafka.Consumers;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<NotificationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("NotificationDb")));

builder.Services.AddHostedService<PaymentResultConsumer>();

var app = builder.Build();

app.UseHttpsRedirection();

app.Run();
