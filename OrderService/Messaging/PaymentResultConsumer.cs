using Confluent.Kafka;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using OrdersService.Data;
using OrdersService.Models;
using System.Text.Json;

namespace OrdersService.Messaging;
public class PaymentResultConsumer : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConsumer<string, string> _consumer;
    public PaymentResultConsumer(IServiceScopeFactory scopeFactory, IConfiguration config)
    {
        _scopeFactory = scopeFactory;
        var cfg = new ConsumerConfig
        {
            BootstrapServers = config["Kafka:BootstrapServers"],
            GroupId = "orders-service-consumer",
            AutoOffsetReset = AutoOffsetReset.Earliest
        };
        _consumer = new ConsumerBuilder<string, string>(cfg).Build();
        _consumer.Subscribe("orders.payment-result");
    }
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var cr = _consumer.Consume(stoppingToken);
            var msg = JsonSerializer.Deserialize<PaymentResult>(cr.Message.Value);
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<OrdersDbContext>();
            var order = await db.Orders.FindAsync(msg!.OrderId);
            if (order != null)
            {
                order.Status = msg.Success ? OrderStatus.FINISHED : OrderStatus.CANCELLED;
                await db.SaveChangesAsync(stoppingToken);
            }
        }
    }
}

public record PaymentResult(Guid OrderId, bool Success);