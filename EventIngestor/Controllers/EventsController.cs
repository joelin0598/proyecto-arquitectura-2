using Microsoft.AspNetCore.Mvc;
using EventIngestor.Models;
using EventIngestor.Services;
using StackExchange.Redis;
using System.Text.Json;
using Prometheus; // 👈 NUEVO: Agregar este using

namespace EventIngestor.Controllers
{
    [ApiController]
    [Route("events")]
    public class EventsController : ControllerBase
    {
        private readonly IDatabase _redisDatabase;
        
        // 👇 NUEVO: Métricas personalizadas de Prometheus
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
                    Buckets = Histogram.ExponentialBuckets(0.01, 2, 10) // 10ms a ~5s
                });

        public EventsController(IConnectionMultiplexer redisConnection)
        {
            _redisDatabase = redisConnection.GetDatabase();
        }

        [HttpPost]
        public async Task<IActionResult> IngestEvent([FromBody] CanonicalEvent evento)
        {
            // 👇 NUEVO: Medir tiempo de procesamiento
            using (_eventProcessingTime.WithLabels(evento.type ?? "unknown").NewTimer())
            {
                try
                {
                    // Enriquecer campos si vienen vacíos
                    if (string.IsNullOrWhiteSpace(evento.event_id))
                        evento.event_id = Guid.NewGuid().ToString();

                    if (string.IsNullOrWhiteSpace(evento.timestamp))
                        evento.timestamp = DateTime.UtcNow.ToString("o"); // ISO 8601

                    if (string.IsNullOrWhiteSpace(evento.partition_key))
                        evento.partition_key = evento.geo?.zone ?? "default-zone";

                    // Serializar evento a JSON
                    var jsonEvento = JsonSerializer.Serialize(evento);

                    // Usamos correlation_id como la clave para la lista de Redis
                    var redisKey = $"events";

                    // Agregar el evento a la lista de Redis (usamos ListRightPush para agregar al final)
                    await _redisDatabase.ListRightPushAsync(redisKey, jsonEvento);

                    // Publicar en Kafka
                    var producer = new KafkaProducer();
                    try
                    {
                        await producer.PublishAsync(evento);
                        
                        // 👇 NUEVO: Métrica de evento exitoso
                        _eventsReceived
                            .WithLabels(evento.type ?? "unknown", 
                                      evento.geo?.zone ?? "unknown", 
                                      "success")
                            .Inc();
                    }
                    catch (Exception ex)
                    {
                        // 👇 NUEVO: Métrica de evento con error en Kafka
                        _eventsReceived
                            .WithLabels(evento.type ?? "unknown", 
                                      evento.geo?.zone ?? "unknown", 
                                      "kafka_error")
                            .Inc();
                            
                        Console.WriteLine($"[WARN] No se pudo conectar a Kafka: {ex.Message}");
                    }

                    return Accepted(new { message = "Evento publicado en Kafka", evento });
                }
                catch (Exception ex)
                {
                    // 👇 NUEVO: Métrica de error general
                    _eventsReceived
                        .WithLabels(evento.type ?? "unknown", 
                                  evento.geo?.zone ?? "unknown", 
                                  "error")
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
