using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace EventProcessor.Models
{
    [Table("events")]
    public class EventEntity
    {
        [Key]
        [Column("event_id")]
        public Guid EventId { get; set; }

        [Required]
        [Column("event_type")]
        public string EventType { get; set; }

        [Required]
        [Column("event_version")]
        public string EventVersion { get; set; }

        [Required]
        [Column("producer")]
        public string Producer { get; set; }

        [Required]
        [Column("source")]
        public string Source { get; set; }

        [Column("correlation_id")]
        public Guid? CorrelationId { get; set; }

        [Column("trace_id")]
        public Guid? TraceId { get; set; }

        [Required]
        [Column("partition_key")]
        public string PartitionKey { get; set; }

        [Required]
        [Column("ts_utc")]
        public DateTimeOffset TsUtc { get; set; }

        [Column("zone")]
        public string Zone { get; set; }

        [Column("geo_lat")]
        public double? GeoLat { get; set; }

        [Column("geo_lon")]
        public double? GeoLon { get; set; }

        [Column("severity")]
        public string Severity { get; set; }

        [Required]
        [Column("payload")]
        public string Payload { get; set; }
    }
}