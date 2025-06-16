using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using PaymentsService.Data;

namespace PaymentsService.Extensions;
public static class HostExtensions
{
    public static WebApplication MigrateDatabase(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PaymentsDbContext>();
        db.Database.Migrate();
        return app;
    }
}
