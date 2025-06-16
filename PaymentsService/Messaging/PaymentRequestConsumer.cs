using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using PaymentsService.Data;
using PaymentsService.Models;
using StackExchange.Redis;
using System.Text.Json;

namespace PaymentsService.Messaging;

public class PaymentRequestSubscriber : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ISubscriber _sub;
    public PaymentRequestSubscriber(IServiceScopeFactory scopeFactory, IConnectionMultiplexer mux)
    {
        _scopeFactory = scopeFactory;
        _sub = mux.GetSubscriber();
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _sub.Subscribe("orders.created", async (channel, value) =>
        {
            var req = JsonSerializer.Deserialize<PaymentRequest>(value!);
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<PaymentsDbContext>();

            // Сохраняем в inbox_events
            db.InboxEvents.Add(new InboxEvent
            {
                Topic = channel,
                Payload = value!
            });
            await db.SaveChangesAsync(stoppingToken);

            // Пытаемся списать средства
            bool success = false;
            var account = await db.Accounts
                                  .SingleOrDefaultAsync(a => a.UserId == req!.UserId, stoppingToken);
            if (account != null)
            {
                account.Balance -= req.Amount;
                if (account.Balance >= 0)
                {
                    await db.SaveChangesAsync(stoppingToken);
                    success = true;
                }
                else
                {
                    account.Balance += req.Amount; // откат
                }
            }

            // Публикуем результат в outbox
            var result = new PaymentResultEvent(req.OrderId, success);
            db.OutboxEvents.Add(new OutboxEvent
            {
                Topic = "orders.payment-result",
                Payload = JsonSerializer.Serialize(result)
            });
            await db.SaveChangesAsync(stoppingToken);
        });
        return Task.CompletedTask;
    }
}