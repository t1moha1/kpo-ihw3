using Microsoft.EntityFrameworkCore;
using PaymentsService.Data;
using PaymentsService.Messaging;

var builder = WebApplication.CreateBuilder(args);

// Database
builder.Services.AddDbContext<PaymentsDbContext>(opt =>
    opt.UseNpgsql(builder.Configuration.GetConnectionString("PaymentsDb")));

// Kafka
builder.Services.AddSingleton<IMessageProducer, KafkaProducer>();
builder.Services.AddHostedService<OutboxPublisher>();
builder.Services.AddHostedService<PaymentRequestConsumer>();

// Controllers & Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();
app.UseSwagger();
app.UseSwaggerUI();
app.MapControllers();

app.MigrateDatabase();
app.Run();