using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using OrdersService.Data;
using OrdersService.Models;
using StackExchange.Redis;
using System.Text.Json;

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

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Подписываемся на канал результатов оплаты
            _subscriber.Subscribe("orders.payment-result", async (channel, value) =>
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

            // Не завершаем сервис, оставляем подписку живой
            return Task.Delay(-1, stoppingToken);
        }
    }
}