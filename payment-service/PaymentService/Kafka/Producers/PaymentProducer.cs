using Confluent.Kafka;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;


namespace PaymentService.Kafka.Producers;

public class PaymentProducer
{
    private readonly IProducer<string, string> _producer;

    public PaymentProducer()
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
    object value,
    Dictionary<string, string>? headers = null)
    {
        var message = new Message<string, string>
        {
            Key = key,
            Value = JsonSerializer.Serialize(value),
            Headers = new Headers()
        };

        if (headers != null)
        {
            foreach (var h in headers)
            {
                message.Headers.Add(
                    h.Key,
                    System.Text.Encoding.UTF8.GetBytes(h.Value));
            }
        }

        await _producer.ProduceAsync(topic, message);
    }

    //public async Task PublishAsync(string topic, string key, object message)
    //{
    //    var payload = JsonSerializer.Serialize(message);

    //    await _producer.ProduceAsync(topic, new Message<string, string>
    //    {
    //        Key = key,
    //        Value = payload
    //    });
    //}
}
