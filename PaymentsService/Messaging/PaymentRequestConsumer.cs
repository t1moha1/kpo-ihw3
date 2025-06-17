using System.Threading;
using System.Threading.Tasks;
using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using PaymentsService.Data;
using PaymentsService.Models;
using StackExchange.Redis;

namespace PaymentsService.Messaging;

public class PaymentRequestSubscriber : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ISubscriber _subscriber;

    public PaymentRequestSubscriber(IServiceScopeFactory scopeFactory, IConnectionMultiplexer mux)
    {
        _scopeFactory = scopeFactory;
        _subscriber = mux.GetSubscriber();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _subscriber.Subscribe(
            new RedisChannel("orders.created", RedisChannel.PatternMode.Literal),
            async (channel, value) =>
        {
            var req = JsonSerializer.Deserialize<PaymentRequest>(value!);
            if (req is null) return;

            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<PaymentsDbContext>();

            db.InboxEvents.Add(new InboxEvent
            {
                Topic = channel,
                Payload = value!
            });
            await db.SaveChangesAsync(stoppingToken);

            bool success = false;
            var account = await db.Accounts
                                  .SingleOrDefaultAsync(a => a.UserId == req.UserId, stoppingToken);
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
                    account.Balance += req.Amount; 
                }
            }

            var result = new PaymentResultEvent(req.OrderId, success);
            db.OutboxEvents.Add(new OutboxEvent
            {
                Topic = "orders.payment-result",
                Payload = JsonSerializer.Serialize(result)
            });
            await db.SaveChangesAsync(stoppingToken);
        });

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }
}