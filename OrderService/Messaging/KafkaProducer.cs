using Confluent.Kafka;

namespace OrdersService.Messaging;
public interface IMessageProducer { Task ProduceAsync(string topic, string message); }
public class KafkaProducer : IMessageProducer
{
    private readonly IProducer<string, string> _producer;
    public KafkaProducer(IConfiguration config)
    {
        var cfg = new ProducerConfig { BootstrapServers = config["Kafka:BootstrapServers"] };
        _producer = new ProducerBuilder<string, string>(cfg).Build();
    }
    public async Task ProduceAsync(string topic, string message)
        => await _producer.ProduceAsync(topic, new Message<string, string> { Key = null, Value = message });
}