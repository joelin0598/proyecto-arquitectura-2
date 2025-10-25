using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using StackExchange.Redis;
using Correlator.Data;
using EventProcessor.Services;
using Prometheus; // 👈 Para métricas

var builder = Host.CreateApplicationBuilder(args);

// ✅ Configurar métricas Prometheus
builder.Services.AddHealthChecks();

// ✅ Base de datos Postgres
builder.Services.AddDbContext<AppDbContext>(options =>
    //options.UseNpgsql("Host=db-postgres;Port=5432;Database=alertsdb;Username=appuser;Password=appsecret"));
    options.UseNpgsql("Host=arqui-pg.postgres.database.azure.com;Port=5432;Database=test_events;Username=grupo1;Password=4rqu1.4pp"));

// ✅ Servicio Kafka
builder.Services.AddHostedService<KafkaConsumer>();

// ✅ Conexión Redis
string redisConnection = "cache-redis:6379,abortConnect=False";
builder.Services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect(redisConnection));

var app = builder.Build();

// ✅ Iniciar servidor de métricas Prometheus en el puerto 5246
var metricServer = new KestrelMetricServer(port: 5246);
metricServer.Start();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    // db.Database.Migrate();
}

await app.RunAsync();
