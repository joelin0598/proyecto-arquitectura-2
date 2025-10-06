using Microsoft.AspNetCore.Mvc;
using EventIngestor.Models;
using EventIngestor.Services;

namespace EventIngestor.Controllers
{
    [ApiController]
    [Route("events")]
    public class EventsController : ControllerBase
    {
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
