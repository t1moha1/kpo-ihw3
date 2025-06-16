using Microsoft.EntityFrameworkCore;
using PaymentsService.Models;

namespace PaymentsService.Data;
public class PaymentsDbContext : DbContext
{
    public PaymentsDbContext(DbContextOptions<PaymentsDbContext> opts) : base(opts) { }
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<InboxEvent> InboxEvents => Set<InboxEvent>();
    public DbSet<OutboxEvent> OutboxEvents => Set<OutboxEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Account>().ToTable("accounts");
        modelBuilder.Entity<InboxEvent>().ToTable("inbox_events");
        modelBuilder.Entity<OutboxEvent>().ToTable("outbox_events");
        modelBuilder.Entity<Account>()
            .Property(a => a.RowVersion)
            .IsRowVersion();
    }
}