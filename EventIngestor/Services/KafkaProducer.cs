using Confluent.Kafka;
using System.Text.Json;

namespace EventIngestor.Services;

public class KafkaProducer
{
    private readonly string _bootstrapServers = "kafka:9092"; // Puerto externo para clientes fuera del contenedor
                                                              // IP del servidor Kafka en VLAN 10
    private readonly string _topic = "events.standardized";

    public async Task PublishAsync(object evento)
    {
        var config = new ProducerConfig
        {
            BootstrapServers = _bootstrapServers
        };

        using var producer = new ProducerBuilder<Null, string>(config).Build();

        var json = JsonSerializer.Serialize(evento);

        var message = new Message<Null, string> { Value = json };

        var deliveryResult = await producer.ProduceAsync(_topic, message);

        Console.WriteLine($"Evento publicado en Kafka: {deliveryResult.TopicPartitionOffset}");
    }
}
