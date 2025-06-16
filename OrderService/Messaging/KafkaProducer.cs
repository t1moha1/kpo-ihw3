using StackExchange.Redis;
namespace OrdersService.Messaging;

public interface IMessageProducer { Task ProduceAsync(string topic, string message); }

public class RedisProducer : IMessageProducer
{
    private readonly ISubscriber _sub;
    public RedisProducer(IConnectionMultiplexer mux)
    {
        _sub = mux.GetSubscriber();
    }

    public Task ProduceAsync(string topic, string message) =>
        _sub.PublishAsync(topic, message);
}