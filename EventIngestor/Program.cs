var builder = WebApplication.CreateBuilder(args);

// 👇 Forzar puerto dentro del contenedor
builder.WebHost.UseUrls("http://*:5245");

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddControllers();

var app = builder.Build();

// 👇 Habilitar Swagger sin importar el entorno
app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();
app.MapControllers();
app.Run();
