using StackExchange.Redis;
using Prometheus;
using Nest;
using EventIngestor.Services;


var builder = WebApplication.CreateBuilder(args);

// 👇 Forzar puerto dentro del contenedor
builder.WebHost.UseUrls("http://*:5245");

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddControllers();
builder.Services.AddHealthChecks();

// ✅ Redis
string redisConnection = "cache-redis:6379,abortConnect=False";
builder.Services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect(redisConnection));

// ✅ Elasticsearch
var esSettings = new ConnectionSettings(new Uri("http://elasticsearch:9200"))
    .DefaultIndex("events");

var esClient = new ElasticClient(esSettings);
builder.Services.AddSingleton<IElasticClient>(esClient);

// ✅ KafkaProducer con Elasticsearch inyectado
builder.Services.AddSingleton<KafkaProducer>();

var app = builder.Build();

// ✅ Swagger
app.UseSwagger();
app.UseSwaggerUI();

// ✅ Prometheus
app.UseRouting();
app.UseHttpMetrics();
app.UseEndpoints(endpoints =>
{
    endpoints.MapMetrics();
    endpoints.MapHealthChecks("/health");
});

app.UseHttpsRedirection();
app.MapControllers();
app.Run();
