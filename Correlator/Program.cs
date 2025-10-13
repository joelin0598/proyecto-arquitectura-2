using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using StackExchange.Redis;
using Correlator.Data;
using EventProcessor.Services;
using Prometheus; // 👈 Nuevo

var builder = Host.CreateApplicationBuilder(args);

// 👇 Configurar métricas para el Correlator
builder.Services.AddHealthChecks();
builder.Services.AddMetricServer(options =>
{
    options.Port = 5246; // 👈 Puerto diferente para métricas del Correlator
});

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql("Host=db-postgres;Port=5432;Database=alertsdb;Username=appuser;Password=appsecret"));

builder.Services.AddHostedService<KafkaConsumer>();
string redisConnection = "cache-redis:6379,abortConnect=False";
builder.Services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect(redisConnection));

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    // db.Database.Migrate();
}

await app.RunAsync();
