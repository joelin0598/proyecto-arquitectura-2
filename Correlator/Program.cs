using Microsoft.EntityFrameworkCore;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using StackExchange.Redis;
using Correlator.Data;
using EventProcessor.Services;



var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql("Host=db-postgres;Port=5432;Database=alertsdb;Username=appuser;Password=appsecret"));

builder.Services.AddHostedService<KafkaConsumer>();
string redisConnection = "cache-redis:6379,abortConnect=False"; // Asegúrate de poner la conexión correcta a tu servidor Redis
builder.Services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect(redisConnection));

var app = builder.Build();
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    // db.Database.Migrate();
}

await app.RunAsync();
