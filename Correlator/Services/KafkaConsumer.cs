using Confluent.Kafka;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using EventProcessor.Models;
using System.Text.Json;
using StackExchange.Redis;
using Correlator.Services;
using Correlator.Models;
using Correlator.Data;

namespace EventProcessor.Services;

public class KafkaConsumer : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly IConfiguration _config;
    private readonly IDatabase _redisDatabase;

    public KafkaConsumer(IServiceProvider services, IConfiguration config, IConnectionMultiplexer redisConnection)
    {
        _services = services;
        _config = config;
        _redisDatabase = redisConnection.GetDatabase();
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

                var raw = System.Text.Json.JsonSerializer.Deserialize<RawEventDto>(json, new JsonSerializerOptions
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

                    string redisKey = "events";

                    var eventsList = await _redisDatabase.ListRangeAsync(redisKey);

                    if (eventsList.Length > 0)
                    {
                        List<RawEventDto> correlatedEvents = eventsList.Select(x => System.Text.Json.JsonSerializer.Deserialize<RawEventDto>(x)).ToList();
                        await CorrelateEventsByDistance(correlatedEvents, raw);
                    }

                    var evento = new _event
                    {
                        event_id = Guid.NewGuid(),
                        event_type = raw.event_type,
                        producer = raw.producer,
                        source = raw.source,
                        correlation_id = Guid.Parse(raw.correlation_id),
                        trace_id = Guid.Parse(raw.trace_id),
                        partition_key = raw.partition_key,
                        ts_utc = DateTime.Now.ToUniversalTime(),
                        zone = raw.geo.zone,
                        geo_lat = (decimal)raw.geo.lat,
                        geo_lon = (decimal)raw.geo.lon,
                        severity = raw.severity,
                        payload = System.Text.Json.JsonSerializer.Serialize(raw.payload),
                        event_version = raw.event_version,
                    };


                    using var scope = _services.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                    db.events.Add(evento);
                    await db.SaveChangesAsync();

                    Console.WriteLine($"✅ Evento guardado: {evento.event_id} | Zona: {evento.zone} | Tipo: {raw.event_type}");
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

    public async Task PublishAsync(object evento)
    {
        const string _topic = "correlated.alerts";
        var config = new ProducerConfig
        {
            BootstrapServers = _config["Kafka:BootstrapServers"]
        };

        using var producer = new ProducerBuilder<Null, string>(config).Build();

        var json = System.Text.Json.JsonSerializer.Serialize(evento);

        var message = new Message<Null, string> { Value = json };

        var deliveryResult = await producer.ProduceAsync(_topic, message);

        Console.WriteLine($"Alerta publicado en Kafka: {deliveryResult.TopicPartitionOffset}");
    }

    private async Task CorrelateEventsByDistance(List<RawEventDto> correlatedEvents, RawEventDto newEvent)
    {
        const double distanceThreshold = 1.0;

        var eventsInSameZone = correlatedEvents
            .Where(e => GeoService.Haversine(e.geo?.lat ?? 0, e.geo?.lon ?? 0, newEvent.geo?.lat ?? 0, newEvent.geo?.lon ?? 0) <= distanceThreshold && (e.severity == "warning" || e.severity == "critical"))
            .ToList();

        if (eventsInSameZone.Count >= 5)
        {
            Console.WriteLine($"⚠️ Más de 5 eventos cercanos (distancia <= {distanceThreshold} km) generados. Generando alerta...");
            await makeAlert(eventsInSameZone);
        }

        if (newEvent.severity == "critical")
        {
            Console.WriteLine($"⚠️ Evento másizo (alta severidad) detectado: {newEvent.event_type} | Generando alerta...");
            await makeAlert(eventsInSameZone);
        }
    }

    private async Task makeAlert(List<RawEventDto> events)
    {
        if (events.Count > 0)
        {
            string correlation;
            if (events[0].correlation_id != null)
            {
                correlation = events[0].correlation_id;
            }
            else
            {
                correlation = Guid.NewGuid().ToString();
            }
            DateTime minTimestamp = events
              .Where(e => DateTime.TryParse(e.timestamp, out _)) // Filtra los eventos con un timestamp válido
              .Min(e => DateTime.Parse(e.timestamp));

            DateTime maxTimestamp = events
                .Where(e => DateTime.TryParse(e.timestamp, out _)) // Filtra los eventos con un timestamp válido
                .Max(e => DateTime.Parse(e.timestamp));

            alert alert = new alert()
            {
                alert_id = Guid.NewGuid(),
                correlation_id = Guid.Parse(correlation),
                type = Utils.DetermineAlertType(events),
                score = (decimal)Utils.CalculateAlertScore(events),
                window_start = minTimestamp.ToUniversalTime(),
                window_end = maxTimestamp.ToUniversalTime(),
                evidence = System.Text.Json.JsonSerializer.Serialize(events.Select(e => e.correlation_id)),
                created_at = DateTime.Now.ToUniversalTime()
            };
            await PublishAsync(alert);

            using var scope = _services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.alerts.Add(alert);
            await db.SaveChangesAsync();
        }
    }
}
