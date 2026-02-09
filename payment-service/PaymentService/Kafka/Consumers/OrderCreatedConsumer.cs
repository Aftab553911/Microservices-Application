using Confluent.Kafka;
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

namespace PaymentService.Kafka.Consumers;

public class OrderCreatedConsumer : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly PaymentProducer _producer;

    public OrderCreatedConsumer(
        IServiceScopeFactory scopeFactory,
        PaymentProducer producer)
    {
        _scopeFactory = scopeFactory;
        _producer = producer;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = "kafka:9092",
            GroupId = "payment-service",
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = true
        };

        using var consumer = new ConsumerBuilder<string, string>(config).Build();
        consumer.Subscribe("order.created");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var result = consumer.Consume(stoppingToken);

                var order = JsonSerializer.Deserialize<OrderCreatedEvent>(
                    result.Message.Value);

                if (order == null)
                    continue;

                // 🔑 CREATE SCOPE PER MESSAGE
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider
                              .GetRequiredService<PaymentDbContext>();

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
            }
            catch (ConsumeException ex)
            {
                Console.WriteLine($"Kafka consume error: {ex.Error.Reason}");
                await Task.Delay(2000, stoppingToken);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"PaymentService error: {ex.Message}");
                await Task.Delay(2000, stoppingToken);
            }
        }
    }
}
