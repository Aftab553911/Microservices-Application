using Confluent.Kafka;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace NotificationService.Kafka.Producers;

public class NotificationProducer
{
    private readonly IProducer<string, string> _producer;

    public NotificationProducer()
    {
        var config = new ProducerConfig
        {
            BootstrapServers = "kafka:9092"
        };

        _producer = new ProducerBuilder<string, string>(config).Build();
    }

    public async Task PublishAsync(
        string topic,
        string key,
        string value,
        Dictionary<string, string>? headers = null)
    {
        var message = new Message<string, string>
        {
            Key = key,
            Value = value
        };

        if (headers != null)
        {
            message.Headers = new Headers();
            foreach (var h in headers)
                message.Headers.Add(h.Key, System.Text.Encoding.UTF8.GetBytes(h.Value));
        }

        await _producer.ProduceAsync(topic, message);
    }
}
