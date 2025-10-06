namespace EventProcessor.Models
{
    public class EventEntity
    {
 public Guid Id { get; set; } // event_id
    public required string EventType { get; set; }
    public required string Producer { get; set; }
    public required string Source { get; set; }
    public required string CorrelationId { get; set; }
    public required string TraceId { get; set; }
    public DateTime Timestamp { get; set; }
    public required string PartitionKey { get; set; }
    public required string GeoZone { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public required string Severity { get; set; }
    public required string AlertType { get; set; }
    public required string DeviceId { get; set; }
    public required string UserContext { get; set; }
    }
}