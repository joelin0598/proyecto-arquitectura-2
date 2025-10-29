using Confluent.Kafka;
using System.Text.Json;
using Nest; //Para Elasticsearch
using EventIngestor.Models;

namespace EventIngestor.Services
{
    public class KafkaProducer
    {
        private readonly string _bootstrapServers = "kafka:9092";
        private readonly string _topic = "events.standardized";
        private readonly IElasticClient _esClient;

        public KafkaProducer(IElasticClient esClient)
        {
            _esClient = esClient;
        }

        public async Task PublishAsync(CanonicalEvent evento)
        {
            var config = new ProducerConfig
            {
                BootstrapServers = _bootstrapServers
            };

            using var producer = new ProducerBuilder<Null, string>(config).Build();

            var json = JsonSerializer.Serialize(evento);
            var message = new Message<Null, string> { Value = json };

            var deliveryResult = await producer.ProduceAsync(_topic, message);

            Console.WriteLine($"✅ Evento publicado en Kafka: {deliveryResult.TopicPartitionOffset}");

            //Lógica para indexar en Elasticsearch
            var indexRequest = new IndexRequest<object>(IndexName.From<object>("events"))
            {
                Document = new {
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
                }
            };

            var response = await _esClient.IndexAsync(indexRequest);

            Console.WriteLine($"📦 Indexación: {response.Result}, ID: {response.Id}");
        }
    }
}
