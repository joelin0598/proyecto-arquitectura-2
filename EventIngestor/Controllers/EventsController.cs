using Microsoft.AspNetCore.Mvc;
using EventIngestor.Models;
using EventIngestor.Services;
using StackExchange.Redis;
using System.Text.Json;
using Prometheus;
using Metrics = Prometheus.Metrics; // 👈 Desambiguar
using Nest; // 👈 NUEVO: Elasticsearch

namespace EventIngestor.Controllers
{
    [ApiController]
    [Route("events")]
    public class EventsController : ControllerBase
    {
        private readonly IDatabase _redisDatabase;
        private readonly IElasticClient _esClient;
        private readonly KafkaProducer _producer;

        // 👇 Métricas personalizadas de Prometheus
        private static readonly Counter _eventsReceived = Metrics
            .CreateCounter("urbanevents_events_received_total", "Total de eventos recibidos", 
                new CounterConfiguration
                {
                    LabelNames = new[] { "event_type", "location", "status" }
                });

        private static readonly Histogram _eventProcessingTime = Metrics
            .CreateHistogram("urbanevents_event_processing_seconds", "Tiempo de procesamiento de eventos",
                new HistogramConfiguration
                {
                    LabelNames = new[] { "event_type" },
                    Buckets = Histogram.ExponentialBuckets(0.01, 2, 10)
                });

        public EventsController(IConnectionMultiplexer redisConnection, IElasticClient esClient, KafkaProducer producer)
        {
            _redisDatabase = redisConnection.GetDatabase();
            _esClient = esClient;
            _producer = producer;
        }

        [HttpPost]
        public async Task<IActionResult> IngestEvent([FromBody] CanonicalEvent evento)
        {
            using (_eventProcessingTime.WithLabels(evento.event_type ?? "unknown").NewTimer())
            {
                try
                {
                    // Enriquecer campos si vienen vacíos
                    if (string.IsNullOrWhiteSpace(evento.event_id))
                        evento.event_id = Guid.NewGuid().ToString();

                    if (string.IsNullOrWhiteSpace(evento.timestamp))
                        evento.timestamp = DateTime.UtcNow.ToString("o");

                    if (string.IsNullOrWhiteSpace(evento.partition_key))
                        evento.partition_key = evento.geo?.zone ?? "default-zone";

                    var jsonEvento = JsonSerializer.Serialize(evento);
                    var redisKey = $"events_{evento.geo?.zone ?? "unknown"}";

                    // ✅ Verificar si el evento ya existe por event_id
                    var existingEvents = await _redisDatabase.ListRangeAsync(redisKey);
                    bool alreadyExists = existingEvents.Any(x => x.ToString().Contains(evento.event_id));

                    if (!alreadyExists)
                    {
                        await _redisDatabase.ListRightPushAsync(redisKey, jsonEvento);

                        // ✅ Agregar TTL de 10 minutos si es la primera vez que se escribe
                        if (existingEvents.Length == 0)
                        {
                            await _redisDatabase.KeyExpireAsync(redisKey, TimeSpan.FromMinutes(10));
                        }
                    }

                    
                    try
                    {
                        await _producer.PublishAsync(evento);


                        // 👇 Publicar en Elasticsearch
                        await _esClient.IndexDocumentAsync(new {
                            event_id = evento.event_id,
                            event_type = evento.event_type,
                            timestamp = evento.timestamp,
                            geo = new {
                                zone = evento.geo?.zone,
                                lat = evento.geo?.lat,
                                lon = evento.geo?.lon
                            },
                            severity = evento.severity,
                            payload = evento.payload
                        });

                        _eventsReceived
                            .WithLabels(evento.event_type ?? "unknown", evento.geo?.zone ?? "unknown", "success")
                            .Inc();
                    }
                    catch (Exception ex)
                    {
                        _eventsReceived
                            .WithLabels(evento.event_type ?? "unknown", evento.geo?.zone ?? "unknown", "kafka_error")
                            .Inc();

                        Console.WriteLine($"[WARN] No se pudo conectar a Kafka: {ex.Message}");
                    }

                    return Accepted(new { message = "Evento publicado en Kafka y Elasticsearch", evento });
                }
                catch (Exception ex)
                {
                    _eventsReceived
                        .WithLabels(evento.event_type ?? "unknown", evento.geo?.zone ?? "unknown", "error")
                        .Inc();

                    Console.WriteLine($"[ERROR] Error procesando evento: {ex.Message}");
                    return StatusCode(500, new { error = "Error interno del servidor" });
                }
            }
        }

        [HttpGet("health")]
        public IActionResult HealthCheck()
        {
            return Ok("Event Ingestor is alive");
        }
    }
}
