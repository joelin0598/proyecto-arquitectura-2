using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

// 👇 Forzar puerto dentro del contenedor
builder.WebHost.UseUrls("http://*:5245");

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddControllers();
string redisConnection = "localhost"; // Asegúrate de poner la conexión correcta a tu servidor Redis
builder.Services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect(redisConnection));
var app = builder.Build();

// 👇 Habilitar Swagger sin importar el entorno
app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();
app.MapControllers();
app.Run();
