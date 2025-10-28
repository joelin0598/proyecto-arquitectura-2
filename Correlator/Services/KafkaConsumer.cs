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
using Prometheus;
using Metrics = Prometheus.Metrics; // 👈 Esto desambiguará el uso
using Nest;
using Microsoft.Extensions.Logging;



namespace EventProcessor.Services;

public class KafkaConsumer : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly IConfiguration _config;
    private readonly IDatabase _redisDatabase;

    // 👇 NUEVO: Métricas personalizadas de Prometheus
    private static readonly Counter _alertsGenerated = Metrics
        .CreateCounter("urbanevents_alerts_generated_total", "Total de alertas generadas",
            new CounterConfiguration
            {
                LabelNames = new[] { "alert_type", "severity_level", "location" }
            });

    private static readonly Counter _eventsProcessed = Metrics
        .CreateCounter("urbanevents_events_processed_total", "Total de eventos procesados por el correlator",
            new CounterConfiguration
            {
                LabelNames = new[] { "event_type", "processing_result" }
            });

    private static readonly Histogram _alertProcessingTime = Metrics
        .CreateHistogram("urbanevents_alert_processing_seconds", "Tiempo de procesamiento de correlación",
            new HistogramConfiguration
            {
                LabelNames = new[] { "alert_type" },
                Buckets = Histogram.ExponentialBuckets(0.01, 2, 10)
            });

    private static readonly Gauge _activeEventsInZone = Metrics
        .CreateGauge("urbanevents_active_events_in_zone", "Eventos activos por zona para correlación",
            new GaugeConfiguration
            {
                LabelNames = new[] { "zone" }
            });

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
            BootstrapServers = "kafka:9092",
            GroupId = "event-processor-group",
            AutoOffsetReset = AutoOffsetReset.Earliest
        };

        using var consumer = new ConsumerBuilder<Ignore, string>(kafkaConfig).Build();
        consumer.Subscribe("events.standardized");

        Console.WriteLine("🟢 KafkaConsumer iniciado. Escuchando eventos...");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var result = consumer.Consume(stoppingToken);
                var json = result.Message.Value;

                // 👇 Métrica de evento recibido
                _eventsProcessed
                    .WithLabels("unknown", "received")
                    .Inc();

                var raw = System.Text.Json.JsonSerializer.Deserialize<RawEventDto>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (raw is not null)
                {
                    if (raw is null || raw.geo is null || raw.payload is null)
                    {
                        Console.WriteLine("⚠️ Evento inválido: faltan datos esenciales.");
                        _eventsProcessed
                            .WithLabels("unknown", "invalid")
                            .Inc();
                        continue;
                    }

                    // 👇 Métrica de evento procesado exitosamente
                    _eventsProcessed
                        .WithLabels(raw.event_type ?? "unknown", "processed")
                        .Inc();

                    string redisKey = $"events_{raw.geo.zone}";

                    var eventsList = await _redisDatabase.ListRangeAsync(redisKey);

                    // 👇 Actualizar métrica de eventos activos por zona
                    UpdateActiveEventsMetrics(eventsList, raw.geo.zone);

                    if (eventsList.Length > 0)
                    {
                        List<RawEventDto> correlatedEvents = eventsList.Select(x => System.Text.Json.JsonSerializer.Deserialize<RawEventDto>(x)).ToList();

                        DateTime fiveMinutesAgo = DateTime.UtcNow.AddMinutes(-5);
                        correlatedEvents = correlatedEvents.Where(x => DateTime.TryParse(x.timestamp, out DateTime timestamp) && timestamp >= fiveMinutesAgo).ToList();
                        await _redisDatabase.KeyDeleteAsync(redisKey);
                        var listSerialize = correlatedEvents.Select(x => JsonSerializer.Serialize(x));
                        await _redisDatabase.ListRightPushAsync(redisKey, listSerialize.Select(x => (RedisValue)x).ToArray());

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

                    var esClient = scope.ServiceProvider.GetRequiredService<IElasticClient>();

                    await esClient.IndexDocumentAsync(new {
                        event_id = evento.event_id,
                        event_type = evento.event_type,
                        timestamp = evento.ts_utc,
                        geo = new { zone = evento.zone, lat = evento.geo_lat, lon = evento.geo_lon },
                        severity = evento.severity,
                        payload = evento.payload
                    });


                    Console.WriteLine($"✅ Evento guardado: {evento.event_id} | Zona: {evento.zone} | Tipo: {raw.event_type}");
                }
                else
                {
                    Console.WriteLine("⚠️ Evento nulo: el JSON no se pudo deserializar correctamente.");
                    _eventsProcessed
                        .WithLabels("unknown", "deserialization_error")
                        .Inc();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error al consumir evento: {ex.Message}");
                _eventsProcessed
                    .WithLabels("unknown", "error")
                    .Inc();
            }
        }

        consumer.Close();
    }
    
    

    public async Task PublishAsync(object evento)
    {
        const string _topic = "correlated.alerts";
        var config = new ProducerConfig
        {
            BootstrapServers = "kafka:9092"
        };

        using var producer = new ProducerBuilder<Null, string>(config).Build();

        var json = System.Text.Json.JsonSerializer.Serialize(evento);

        var message = new Message<Null, string> { Value = json };

        var deliveryResult = await producer.ProduceAsync(_topic, message);

        Console.WriteLine($"Alerta publicado en Kafka: {deliveryResult.TopicPartitionOffset}");
    }

    private async Task CorrelateEventsByDistance(List<RawEventDto> correlatedEvents, RawEventDto newEvent)
    {
        const double distanceThreshold = 5.0;
        List<RawEventDto> eventsInSameZone = correlatedEvents
            .Where(e => GeoService.Haversine(e.geo?.lat ?? 0, e.geo?.lon ?? 0, newEvent.geo?.lat ?? 0, newEvent.geo?.lon ?? 0) <= distanceThreshold && (e.severity == "warning" || e.severity == "critical"))
            .ToList();

        // 👇 Medir tiempo de procesamiento de correlación
        using (_alertProcessingTime.WithLabels(DetermineAlertCategory(newEvent)).NewTimer())
        {
            bool alertGenerated = false;
            string alertType = "";

            if (eventsInSameZone.Count >= 5)
            {
                alertType = Utils.DetermineAlertType(eventsInSameZone);

                Console.WriteLine($"⚠️ Más de 5 eventos cercanos (distancia <= {distanceThreshold} km) generados. Generando alerta...");
                alertType = "multiple_events_cluster";
                await makeAlert(eventsInSameZone,alertType, "high", correlatedEvents);
                alertGenerated = true;
            }

            if (newEvent.severity == "critical" || newEvent.severity == "warning")
            {
                Console.WriteLine($"⚠️ Evento crítico detectado: {newEvent.event_type} | Generando alerta...");
                alertType = Utils.DetermineAlertType(eventsInSameZone);
                await makeAlert(new List<RawEventDto> { newEvent },alertType,"hight", correlatedEvents);
                alertGenerated = true;
            }


            // 👇 Métrica de alerta generada
            if (alertGenerated)
            {
                _alertsGenerated
                    .WithLabels(alertType, 
                              newEvent.severity ?? "medium", 
                              newEvent.geo?.zone ?? "unknown")
                    .Inc();
            }
        }
    }

    private async Task makeAlert(List<RawEventDto> events, string alertType,string severity, List<RawEventDto> rawEventDtos)
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
              .Where(e => DateTime.TryParse(e.timestamp, out _))
              .Min(e => DateTime.Parse(e.timestamp));

            DateTime maxTimestamp = events
                .Where(e => DateTime.TryParse(e.timestamp, out _))
                .Max(e => DateTime.Parse(e.timestamp));

            string redisKey = $"events_{events[0].geo.zone}";
            await _redisDatabase.KeyDeleteAsync(redisKey);
            rawEventDtos.RemoveAll(rawEvent => events.Contains(rawEvent));
            var listSerialize = rawEventDtos.Select(x => JsonSerializer.Serialize(x));
            
            await _redisDatabase.ListRightPushAsync(redisKey, listSerialize.Select(x => (RedisValue)x).ToArray());
            alert alert = new alert()
            {
                alert_id = Guid.NewGuid(),
                correlation_id = Guid.Parse(correlation),
                type = alertType, // 👈 Usar el tipo específico de alerta
                score = (decimal)Utils.CalculateAlertScore(events),
                window_start = minTimestamp.ToUniversalTime(),
                window_end = maxTimestamp.ToUniversalTime(),
                evidence = System.Text.Json.JsonSerializer.Serialize(events.Select(e => e.correlation_id)),
                created_at = DateTime.Now.ToUniversalTime(),
                zone = events[0].geo.zone 
            };
            await PublishAsync(alert);

            using var scope = _services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.alerts.Add(alert);
            await db.SaveChangesAsync();

            Console.WriteLine($"🚨 ALERTA GENERADA: {alertType} | Severidad: {severity} | Zona: {events[0].geo?.zone}");
        }
    }

    // 👇 NUEVO: Métodos auxiliares para clasificación de eventos graves
    private bool IsCriticalEventType(RawEventDto eventData)
    {
        var criticalTypes = new[] { "fire", "incendio", "accident", "accidente", "shooting", "disparo", "explosion", "explosión", "earthquake", "terremoto" };
        return criticalTypes.Any(type => 
            (eventData.event_type?.ToLower().Contains(type) == true) ||
            (eventData.payload?.ToString()?.ToLower().Contains(type) == true));
    }

    private string DetermineAlertCategory(RawEventDto eventData)
    {
        if (IsCriticalEventType(eventData))
            return "critical_emergency";
        if (eventData.severity == "critical")
            return "high_severity";
        return "general_alert";
    }

    private void UpdateActiveEventsMetrics(StackExchange.Redis.RedisValue[] eventsList, string zone)
    {
        try
        {
            var eventsInZone = eventsList
                .Where(x => !x.IsNullOrEmpty)
                .Select(x => {
                    try {
                        return System.Text.Json.JsonSerializer.Deserialize<RawEventDto>(x);
                    } catch {
                        return null;
                    }
                })
                .Where(x => x != null && x.geo?.zone == zone)
                .Count();

            _activeEventsInZone
                .WithLabels(zone ?? "unknown")
                .Set(eventsInZone);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ Error actualizando métricas de eventos activos: {ex.Message}");
        }
    }
}