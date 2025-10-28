using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using StackExchange.Redis;
using Correlator.Data;
using EventProcessor.Services;
using Prometheus; // 👈 Para métricas
using Nest;

var builder = Host.CreateApplicationBuilder(args);

// ✅ Configurar métricas Prometheus
builder.Services.AddHealthChecks();

// ✅ Base de datos Postgres
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql("Host=db-postgres;Port=5432;Database=alertsdb;Username=appuser;Password=appsecret"));

// ✅ Servicio Kafka
builder.Services.AddHostedService<KafkaConsumer>();

// ✅ Conexión Redis
string redisConnection = "cache-redis:6379,abortConnect=False";
builder.Services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect(redisConnection));

//metricas Elasticsearch
var settings = new ConnectionSettings(new Uri("http://elasticsearch:9200"))
    .DefaultIndex("events");

var esClient = new ElasticClient(settings);
builder.Services.AddSingleton<IElasticClient>(esClient);

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
