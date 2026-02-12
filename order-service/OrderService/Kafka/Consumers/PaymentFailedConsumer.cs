using Confluent.Kafka;
using Microsoft.EntityFrameworkCore;
using OrderService.Data;
using OrderService.Kafka.Producers;
using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace OrderService.Kafka.Consumers;

public class PaymentFailedConsumer : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly OrderCancelledProducer _producer;

    public PaymentFailedConsumer(
        IServiceScopeFactory scopeFactory,
        OrderCancelledProducer producer)
    {
        _scopeFactory = scopeFactory;
        _producer = producer;
    }
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = "kafka:9092",
            GroupId = "order-service",
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false
        };

        using var consumer =
            new ConsumerBuilder<string, string>(config).Build();

        consumer.Subscribe("payment.failed");

        try
        {
            while (true)
            {
                try
                {
                    var result = consumer.Consume(stoppingToken);

                    // process message
                }
                catch (ConsumeException ex)
                {
                    Console.WriteLine($"Kafka error: {ex.Error.Reason}");
                    await Task.Delay(3000);
                }
            }
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("Service stopping gracefully...");
        }
        finally
        {
            consumer.Close();
        }
    }

}
