using Confluent.Kafka;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using EventProcessor.Data;
using EventProcessor.Models;
using System.Text.Json;

namespace EventProcessor.Services;

public class KafkaConsumer : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly IConfiguration _config;

    public KafkaConsumer(IServiceProvider services, IConfiguration config)
    {
        _services = services;
        _config = config;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var kafkaConfig = new ConsumerConfig
        {
            BootstrapServers = _config["Kafka:BootstrapServers"],
            GroupId = "event-processor-group",
            AutoOffsetReset = AutoOffsetReset.Earliest
        };

        using var consumer = new ConsumerBuilder<Ignore, string>(kafkaConfig).Build();
        consumer.Subscribe(_config["Kafka:Topic"]);

        Console.WriteLine("🟢 KafkaConsumer iniciado. Escuchando eventos...");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var result = consumer.Consume(stoppingToken);
                var json = result.Message.Value;

                var raw = JsonSerializer.Deserialize<RawEventDto>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (raw is not null)
                {

                    if (raw is null || raw.geo is null || raw.payload is null)
{
    Console.WriteLine("⚠️ Evento inválido: faltan datos esenciales.");
    return;
}

var evento = new EventEntity
{
    Id = Guid.NewGuid(),
    EventType = raw.event_type,
    Producer = raw.producer,
    Source = raw.source,
    CorrelationId = raw.correlation_id,
    TraceId = raw.trace_id,
    PartitionKey = raw.partition_key,
    GeoZone = raw.geo.zone,
    Latitude = raw.geo.lat,
    Longitude = raw.geo.lon,
    Severity = raw.severity,
    AlertType = raw.payload.tipo_de_alerta,
    DeviceId = raw.payload.identificador_dispositivo,
    UserContext = raw.payload.user_context,
    Timestamp = DateTime.UtcNow
};


                    using var scope = _services.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<EventDbContext>();

                    db.Events.Add(evento);
                    await db.SaveChangesAsync();

                    Console.WriteLine($"✅ Evento guardado: {evento.Id} | Zona: {evento.GeoZone} | Tipo: {evento.AlertType}");
                }
                else
                {
                    Console.WriteLine("⚠️ Evento nulo: el JSON no se pudo deserializar correctamente.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error al consumir evento: {ex.Message}");
            }
        }

        consumer.Close();
    }
}
