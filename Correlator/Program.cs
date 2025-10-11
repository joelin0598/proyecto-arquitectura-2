using Microsoft.EntityFrameworkCore;
using EventProcessor.Data;
using EventProcessor.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using StackExchange.Redis;



var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddDbContext<EventDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("PostgresConnection")));

builder.Services.AddHostedService<KafkaConsumer>();
string redisConnection = "localhost"; // Asegúrate de poner la conexión correcta a tu servidor Redis
builder.Services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect(redisConnection));

var app = builder.Build();
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<EventDbContext>();
    db.Database.Migrate();
}

await app.RunAsync();
