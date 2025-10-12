using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

// 👇 Forzar puerto dentro del contenedor
builder.WebHost.UseUrls("http://*:5245");

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddControllers();
string redisConnection = "cache-redis:6379,abortConnect=False"; // Asegúrate de poner la conexión correcta a tu servidor Redis
//builder.Services.AddStackExchangeRedisCache(option=> option.Configuration =redisConnection);
//var configurationOptions = ConfigurationOptions.Parse(redisConnectionString);
//configurationOptions.AbortOnConnectFail = false; // Permite reintentos automáticos
builder.Services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect(redisConnection));
var app = builder.Build();

// 👇 Habilitar Swagger sin importar el entorno
app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();
app.MapControllers();
app.Run();
