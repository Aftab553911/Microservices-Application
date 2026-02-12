using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NotificationService.Data;
using NotificationService.Models;
using NotificationService.Kafka.Producers;
using System;
using System.Threading;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace NotificationService.Kafka.Consumers;

public class PaymentResultConsumer : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IMemoryCache _cache;
    private readonly NotificationProducer _producer;

    public PaymentResultConsumer(
        IServiceScopeFactory scopeFactory,
        IMemoryCache cache,
        NotificationProducer producer)
    {
        _scopeFactory = scopeFactory;
        _cache = cache;
        _producer = producer;
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

       consumer.Subscribe(new[]
{
    "payment.completed",
    "payment.failed",
    "order.cancelled"
});


        while (!stoppingToken.IsCancellationRequested)
        {  //This is also same concern of result
            ConsumeResult<string, string>? result = null;
            var eventKey=string.Empty;
            var retryCount = 0;
            try
            {
                result = consumer.Consume(stoppingToken);          
                eventKey = result.Message.Key;

                if (string.IsNullOrWhiteSpace(eventKey))
                {
                    Console.WriteLine("Missing event key, skipped");
                    continue;
                }
                 retryCount = GetRetryCount(result);
                // 1️⃣ FAST MEMORY CHECK
                if (_cache.TryGetValue(eventKey, out _))
                {
                    Console.WriteLine($"Duplicate ignored (cache): {eventKey}");
                    continue;
                }
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<NotificationDbContext>();

                // 2️⃣ DB CHECK
                var alreadyProcessed = await db.ProcessedEvents
                    .AnyAsync(e => e.EventKey == eventKey, stoppingToken);

                if (alreadyProcessed)
                {
                    _cache.Set(eventKey, true, TimeSpan.FromMinutes(30));
                    Console.WriteLine($"Duplicate ignored (db): {eventKey}");
                    continue;
                }


                var orderId = result.Message.Key;

                var notification = new Notification
                {
                    Id = Guid.NewGuid(),
                    OrderId = Guid.Parse(orderId),
                    Type = result.Topic,
                    CreatedAt = DateTime.UtcNow
                };

                // 🔥 HANDLE MESSAGE BASED ON TOPIC
                if (result.Topic == "payment.completed")
                {
                    notification.Message =
                        $"Payment completed for Order {orderId}";
                }
                else if (result.Topic == "payment.failed")
                {
                    notification.Message =
                        $"Payment failed for Order {orderId}";
                }
                else if (result.Topic == "order.cancelled")
                {
                    notification.Message =
                        $"Order {orderId} was cancelled due to payment failure";
                }




                db.Notifications.Add(notification);
                await db.SaveChangesAsync(stoppingToken);

                Console.WriteLine($"Notification created for Order {orderId}");
                db.ProcessedEvents.Add(new ProcessedEvent
                {
                    Id = Guid.NewGuid(),
                    EventKey = eventKey,
                    ProcessedAt = DateTime.UtcNow
                });

                await db.SaveChangesAsync(stoppingToken);
                _cache.Set(eventKey, true, TimeSpan.FromMinutes(30));

                // MANUAL COMMIT
                consumer.Commit(result);

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex}");

                if (result?.Message == null)
                {
                    Console.WriteLine("Result is null, skipping retry.");
                    continue;
                }

                retryCount++;

                if (retryCount <= 3)
                {
                    await _producer.PublishAsync(
                        result.Topic,
                        eventKey,
                        result.Message.Value,
                        new Dictionary<string, string>
                        {
                { "retry-count", retryCount.ToString() }
                        });

                    consumer.Commit(result);
                    continue;
                }

                await _producer.PublishAsync(
                    "notification.failed.dlq",
                    eventKey,
                    result.Message.Value);

                consumer.Commit(result);
            }

        }
    }
    private int GetRetryCount(ConsumeResult<string, string> result)
    {
        if (result.Message.Headers.TryGetLastBytes(
            KafkaRetryConstants.RetryHeader, out var value))
        {
            return int.Parse(System.Text.Encoding.UTF8.GetString(value));
        }

        return 0;
    }
}
