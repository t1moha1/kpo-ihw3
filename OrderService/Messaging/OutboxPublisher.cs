using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using OrdersService.Data;
using OrdersService.Models;
using StackExchange.Redis;

namespace OrdersService.Messaging;

public class OutboxPublisher : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ISubscriber _sub;
    public OutboxPublisher(IServiceScopeFactory scopeFactory, IConnectionMultiplexer mux)
    {
        _scopeFactory = scopeFactory;
        _sub = mux.GetSubscriber();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<OrdersDbContext>();
            var pending = await db.OutboxEvents
                                  .Where(e => e.PublishedAt == null)
                                  .ToListAsync(stoppingToken);

            foreach (var evt in pending)
            {
                await _sub.PublishAsync(evt.Topic, evt.Payload);
                evt.PublishedAt = DateTime.UtcNow;
            }

            await db.SaveChangesAsync(stoppingToken);

            await Task.Delay(1000, stoppingToken);
        }
    }
}