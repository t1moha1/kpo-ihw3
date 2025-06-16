using Microsoft.EntityFrameworkCore;
using OrdersService.Models;

namespace OrdersService.Data;
public class OrdersDbContext : DbContext
{
    public OrdersDbContext(DbContextOptions<OrdersDbContext> opts) : base(opts) { }

    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OutboxEvent> OutboxEvents => Set<OutboxEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Order>().ToTable("orders");
        modelBuilder.Entity<OutboxEvent>().ToTable("outbox_events");
    }
}