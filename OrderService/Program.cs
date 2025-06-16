using Microsoft.EntityFrameworkCore;
using OrdersService.Data;
using OrdersService.Messaging;
using OrdersService.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Конфиг из appsettings.json
builder.Services.AddDbContext<OrdersDbContext>(opt =>
    opt.UseNpgsql(builder.Configuration.GetConnectionString("OrdersDb")));

// Kafka Producer + фоновые сервисы
builder.Services.AddSingleton<IMessageProducer, KafkaProducer>();
builder.Services.AddHostedService<OutboxPublisher>();
builder.Services.AddHostedService<PaymentResultConsumer>();

// Controllers, Swagger, JSON
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();
app.UseSwagger();
app.UseSwaggerUI();
app.MapControllers();

// Автоматическая миграция БД
app.MigrateDatabase();
app.Run();