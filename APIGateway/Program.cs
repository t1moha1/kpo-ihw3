using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Подключаем ocelot.json
builder.Configuration
       .AddJsonFile("ocelot.json", optional: false, reloadOnChange: true);

// Регистрируем Ocelot
builder.Services.AddOcelot();

var app = builder.Build();

// Запускаем Ocelot middleware
await app.UseOcelot();

app.Run();