using Confluent.Kafka;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using PaymentsService.Data;
using PaymentsService.Models;
using System.Text.Json;

namespace PaymentsService.Messaging;
public class PaymentRequestConsumer : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConsumer<string, string> _consumer;
    public PaymentRequestConsumer(IServiceScopeFactory scopeFactory, IConfiguration config)
    {
        _scopeFactory = scopeFactory;
        var cfg = new ConsumerConfig
        {
            BootstrapServers = config["Kafka:BootstrapServers"],
            GroupId = "payments-service-request",
            AutoOffsetReset = AutoOffsetReset.Earliest
        };
        _consumer = new ConsumerBuilder<string, string>(cfg).Build();
        _consumer.Subscribe("orders.created");
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var cr = _consumer.Consume(stoppingToken);
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<PaymentsDbContext>();

            // Сохранить в inbox
            var inbox = new InboxEvent { Id = Guid.NewGuid(), Topic = cr.Topic, Payload = cr.Message.Value };
            db.InboxEvents.Add(inbox);
            await db.SaveChangesAsync(stoppingToken);

            // Обработать
            var req = JsonSerializer.Deserialize<PaymentRequest>(cr.Message.Value)!;
            var account = await db.Accounts.SingleOrDefaultAsync(a => a.UserId == req.UserId, stoppingToken);
            bool success = false;

            if (account != null)
            {
                // CAS через concurrency token
                try
                {
                    account.Balance -= req.Amount;
                    if (account.Balance >= 0)
                    {
                        db.Entry(account).Property("RowVersion").OriginalValue = account.RowVersion;
                        await db.SaveChangesAsync(stoppingToken);
                        success = true;
                    }
                }
                catch (DbUpdateConcurrencyException)
                {
                    success = false;
                }
            }

            // Записать результат в outbox
            var result = new PaymentResultEvent(req.OrderId, success);
            var outbox = new OutboxEvent { Id = Guid.NewGuid(), Topic = "orders.payment-result", Payload = JsonSerializer.Serialize(result) };
            db.OutboxEvents.Add(outbox);
            inbox.Processed = true;
            await db.SaveChangesAsync(stoppingToken);
        }
    }
}