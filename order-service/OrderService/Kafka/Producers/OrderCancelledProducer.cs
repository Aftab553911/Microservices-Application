using Confluent.Kafka;
using System;
using System.Text.Json;
using System.Threading.Tasks;

namespace OrderService.Kafka.Producers;

public class OrderCancelledProducer
{
    private readonly IProducer<string, string> _producer;

    public OrderCancelledProducer()
    {
        var config = new ProducerConfig
        {
            BootstrapServers = "kafka:9092"
        };

        _producer = new ProducerBuilder<string, string>(config).Build();
    }

    public async Task PublishAsync(Guid orderId)
    {
        var message = new
        {
            OrderId = orderId
        };

        await _producer.ProduceAsync("order.cancelled",
            new Message<string, string>
            {
                Key = orderId.ToString(),
                Value = JsonSerializer.Serialize(message)
            });
    }
}
