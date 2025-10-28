namespace EventIngestor.Models
{
    public class CanonicalEvent
    {
        public required string event_version { get; set; }
        public required string event_type { get; set; }
        public required string event_id { get; set; }
        public required string producer { get; set; }
        public required string source { get; set; }
        public required string correlation_id { get; set; }
        public required string trace_id { get; set; }
        public required string timestamp { get; set; }
        public required string partition_key { get; set; }
        public required Geo geo { get; set; }
        public required string severity { get; set; }
        public required Dictionary<string, object> payload { get; set; }
    }

    public class Geo
    {
        public required string zone { get; set; }
        public required double lat { get; set; }
        public required double lon { get; set; }
    }
}