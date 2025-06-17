using System;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using OrdersService.Data;
using OrdersService.Models;
using StackExchange.Redis;

namespace OrdersService.Messaging
{
    public record PaymentResult(Guid OrderId, bool Success);

    public class PaymentResultSubscriber : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ISubscriber _subscriber;

        public PaymentResultSubscriber(IServiceScopeFactory scopeFactory, IConnectionMultiplexer mux)
        {
            _scopeFactory = scopeFactory;
            _subscriber = mux.GetSubscriber();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _subscriber.Subscribe(
                new RedisChannel("orders.payment-result", RedisChannel.PatternMode.Literal),
                async (channel, value) =>
            {
                var msg = JsonSerializer.Deserialize<PaymentResult>(value!);
                if (msg is null) return;

                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<OrdersDbContext>();
                var order = await db.Orders.FindAsync(msg.OrderId);
                if (order != null)
                {
                    order.Status = msg.Success
                        ? OrderStatus.FINISHED
                        : OrderStatus.CANCELLED;
                    await db.SaveChangesAsync(stoppingToken);
                }
            });

            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
    }
}