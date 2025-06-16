using Microsoft.EntityFrameworkCore;
using OrdersService.Data;
using OrdersService.Messaging;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

// DbContext
builder.Services.AddDbContext<OrdersDbContext>(opt =>
    opt.UseNpgsql(builder.Configuration.GetConnectionString("OrdersDb")));

// Redis
builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
    ConnectionMultiplexer.Connect(builder.Configuration["Redis:ConnectionString"]));
builder.Services.AddSingleton<IMessageProducer, RedisProducer>();

// Background services
builder.Services.AddHostedService<OutboxPublisher>();
builder.Services.AddHostedService<PaymentResultSubscriber>();

// Controllers, Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Автоматическая миграция
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<OrdersDbContext>();
    db.Database.Migrate();
}

app.UseSwagger();
app.UseSwaggerUI();
app.MapControllers();
app.Run();