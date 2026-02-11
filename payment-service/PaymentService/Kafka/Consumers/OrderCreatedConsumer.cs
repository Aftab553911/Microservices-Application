using Confluent.Kafka;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PaymentService.Data;
using PaymentService.Kafka.Events;
using PaymentService.Kafka.Producers;
using PaymentService.Models;
using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;


namespace PaymentService.Kafka.Consumers;

public class OrderCreatedConsumer : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly PaymentProducer _producer;
    private readonly IMemoryCache _cache;

    public OrderCreatedConsumer(
        IServiceScopeFactory scopeFactory,
        PaymentProducer producer, IMemoryCache cache)
    {
        _scopeFactory = scopeFactory;
        _producer = producer;
         _cache=cache;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = "kafka:9092",
            GroupId = "payment-service",
            AutoOffsetReset = AutoOffsetReset.Earliest,     
            EnableAutoCommit = false
        };

        using var consumer = new ConsumerBuilder<string, string>(config).Build();
        consumer.Subscribe("order.created");

        while (!stoppingToken.IsCancellationRequested)
        {// this is my concern related to result variable, it is being used in catch block but it is declared inside try block, so I moved it outside to be accessible in catch block as well
            ConsumeResult<string, string>? result = null;
            var eventKey=string.Empty;
             var retryCount=0;
            try
            {
                 result = consumer.Consume(stoppingToken);
                 eventKey = result.Message.Key;
                if (string.IsNullOrWhiteSpace(eventKey))
                {
                    Console.WriteLine("Message skipped: missing event key");
                    continue;
                }
                 retryCount = GetRetryCount(result);
                // 1️⃣ FAST CHECK (memory)
                if (_cache.TryGetValue(eventKey, out _))
                {
                    Console.WriteLine($"Duplicate ignored (cache): {eventKey}");
                    continue;
                }
                // 🔑 CREATE SCOPE PER MESSAGE
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider
                              .GetRequiredService<PaymentDbContext>();
                // 2️⃣ DB CHECK (source of truth)
                var alreadyProcessed = await db.ProcessedEvents
                    .AnyAsync(e => e.EventKey == eventKey, stoppingToken);

                if (alreadyProcessed)
                {
                    _cache.Set(eventKey, true, TimeSpan.FromMinutes(30));
                    Console.WriteLine($"Duplicate ignored (db): {eventKey}");
                    continue;
                }
                var order = JsonSerializer.Deserialize<OrderCreatedEvent>(
                   result.Message.Value);

                if (order == null)
                    continue;
                // 1️⃣ Simulate payment
                var success = Random.Shared.Next(0, 100) > 20;

                var payment = new Payment
                {
                    Id = Guid.NewGuid(),
                    OrderId = order.OrderId,
                    Amount = order.TotalAmount,
                    Status = success ? "Completed" : "Failed",
                    CreatedAt = DateTime.UtcNow
                };

                // 2️⃣ SAVE TO DB (SOURCE OF TRUTH)
                db.Payments.Add(payment);
                await db.SaveChangesAsync(stoppingToken);

                // 3️⃣ PUBLISH EVENT AFTER DB COMMIT
                if (success)
                {
                    await _producer.PublishAsync(
                        "payment.completed",
                        order.OrderId.ToString(),
                        new PaymentCompletedEvent
                        {
                            OrderId = order.OrderId,
                            PaidAt = payment.CreatedAt,
                            Status = "Completed"
                        });
                }
                else
                {
                    await _producer.PublishAsync(
                        "payment.failed",
                        order.OrderId.ToString(),
                        new PaymentFailedEvent
                        {
                            OrderId = order.OrderId,
                            FailedAt = payment.CreatedAt
                        });
                }

                // 4️⃣ MARK EVENT AS PROCESSED (IDEMPOTENCY COMMIT)
                db.ProcessedEvents.Add(new ProcessedEvent
                {
                    Id = Guid.NewGuid(),
                    EventKey = eventKey,
                    ProcessedAt = DateTime.UtcNow
                });

                await db.SaveChangesAsync(stoppingToken);
                consumer.Commit(result);

                // Cache it to avoid DB hit next time
                _cache.Set(eventKey, true, TimeSpan.FromMinutes(30));

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");

                if (result == null || result.Message == null)
                {
                    Console.WriteLine("Result is null. Skipping retry.");
                    continue;
                }

                retryCount++;

                if (retryCount <= KafkaRetryConstants.MaxRetryCount)
                {
                    Console.WriteLine($"Retry {retryCount} for order {eventKey}");

                    await _producer.PublishAsync(
                        "order.created",
                        eventKey,
                        result.Message.Value,
                        headers: new Dictionary<string, string>
                        {
                { KafkaRetryConstants.RetryHeader, retryCount.ToString() }
                        });

                    consumer.Commit(result);
                    continue;
                }

                Console.WriteLine($"Sending to DLQ: {eventKey}");

                await _producer.PublishAsync(
                    "payment.failed.dlq",
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
