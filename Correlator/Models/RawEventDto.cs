namespace EventProcessor.Models;
public class RawEventDto
{
    public string? event_type { get; set; }
    public string? correlation_id { get; set; }
    public string? trace_id { get; set; }
    public string? partition_key { get; set; }
    public string? producer { get; set; }
    public string? source { get; set; }
    public string? severity { get; set; }
    public Geo? geo { get; set; }
    public Payload? payload { get; set; }
}

public class Geo
{
    public string? zone { get; set; }
    public double lat { get; set; }
    public double lon { get; set; }
}

public class Payload
{
    public string? tipo_de_alerta { get; set; }
    public string? identificador_dispositivo { get; set; }
    public string? user_context { get; set; }
}
