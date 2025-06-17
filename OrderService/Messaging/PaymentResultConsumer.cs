// File: OrderService/Messaging/PaymentResultSubscriber.cs
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
    // Тип, в который мы десериализуем результат оплаты
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
            // Подписываемся на канал с явным режимом Literal
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

            // Держим сервис живым
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
    }
}