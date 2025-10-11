using Microsoft.AspNetCore.Mvc;
using EventIngestor.Models;
using EventIngestor.Services;
using StackExchange.Redis;
using System.Text.Json;
namespace EventIngestor.Controllers
{
    [ApiController]
    [Route("events")]
    public class EventsController : ControllerBase
    {
        private readonly IDatabase _redisDatabase;

        public EventsController(IConnectionMultiplexer redisConnection)
        {
            _redisDatabase = redisConnection.GetDatabase();
        }

        [HttpPost]
        public async Task<IActionResult> IngestEvent([FromBody] CanonicalEvent evento)
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
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[WARN] No se pudo conectar a Kafka: {ex.Message}");
            }


            return Accepted(new { message = "Evento publicado en Kafka", evento });
        }

        [HttpGet("health")]
        public IActionResult HealthCheck()
        {
            return Ok("Event Ingestor is alive");
        }
    }
}
