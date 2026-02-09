using Confluent.Kafka;
using OrderService.Kafka.Events;
using System.Text.Json;
using System.Threading.Tasks;

namespace OrderService.Kafka.Producers;

public class OrderCreatedProducer
{
    private readonly IProducer<string, string> _producer;
    private const string TopicName = "order.created";

    public OrderCreatedProducer()
    {
        var config = new ProducerConfig
        {
            BootstrapServers = "kafka:9092",
            Acks = Acks.All
        };

        _producer = new ProducerBuilder<string, string>(config).Build();
    }

    public async Task PublishAsync(OrderCreatedEvent orderEvent)
    {
        var message = new Message<string, string>
        {
            Key = orderEvent.OrderId.ToString(),
            Value = JsonSerializer.Serialize(orderEvent)
        };

        await _producer.ProduceAsync(TopicName, message);
    }
}
