using Microsoft.EntityFrameworkCore;
using OrdersService.Data;

namespace OrdersService.Extensions;
public static class HostExtensions
{
    public static IHost MigrateDatabase(this IHost host)
    {
        using var scope = host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OrdersDbContext>();
        db.Database.Migrate();
        return host;
    }
}