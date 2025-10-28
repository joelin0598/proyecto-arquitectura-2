using StackExchange.Redis;
using Prometheus; // 👈 Nuevo

var builder = WebApplication.CreateBuilder(args);

// 👇 Forzar puerto dentro del contenedor
builder.WebHost.UseUrls("http://*:5245");

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddControllers();
builder.Services.AddHealthChecks(); // 👈 Nuevo

string redisConnection = "cache-redis:6379,abortConnect=False";
builder.Services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect(redisConnection));

var app = builder.Build();

// 👇 Habilitar Swagger sin importar el entorno
app.UseSwagger();
app.UseSwaggerUI();

// 👇 Métricas Prometheus (NUEVO)
app.UseRouting();
app.UseHttpMetrics(); // Métricas HTTP automáticas
app.UseEndpoints(endpoints =>
{
    endpoints.MapMetrics(); // 👈 Endpoint /metrics para Prometheus
    endpoints.MapHealthChecks("/health"); // 👈 Health check endpoint
});

app.UseHttpsRedirection();
app.MapControllers();
app.Run();