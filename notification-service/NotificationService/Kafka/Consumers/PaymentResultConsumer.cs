using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NotificationService.Data;
using NotificationService.Models;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace NotificationService.Kafka.Consumers;

public class PaymentResultConsumer : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;

    public PaymentResultConsumer(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = "kafka:9092",
            GroupId = "notification-service",
            AutoOffsetReset = AutoOffsetReset.Earliest
        };

        using var consumer = new ConsumerBuilder<string, string>(config).Build();

        consumer.Subscribe(new[] { "payment.completed", "payment.failed" });

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var result = consumer.Consume(stoppingToken);

                var orderId = result.Message.Key;

                var notification = new Notification
                {
                    Id = Guid.NewGuid(),
                    OrderId = Guid.Parse(orderId),
                    Type = result.Topic,
                    Message = $"Notification sent for {result.Topic}",
                    CreatedAt = DateTime.UtcNow
                };

                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<NotificationDbContext>();

                db.Notifications.Add(notification);
                await db.SaveChangesAsync(stoppingToken);

                Console.WriteLine($"Notification created for Order {orderId}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Notification error: {ex.Message}");
                await Task.Delay(2000, stoppingToken);
            }
        }
    }
}
