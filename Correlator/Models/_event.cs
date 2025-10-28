using System;
using System.Collections.Generic;

namespace Correlator.Models;

public partial class _event
{
    public Guid event_id { get; set; }

    public string event_type { get; set; } = null!;

    public string event_version { get; set; } = null!;

    public string producer { get; set; } = null!;

    public string source { get; set; } = null!;

    public Guid? correlation_id { get; set; }

    public Guid? trace_id { get; set; }

    public string partition_key { get; set; } = null!;

    public DateTime ts_utc { get; set; }

    public string? zone { get; set; }

    public decimal? geo_lat { get; set; }

    public decimal? geo_lon { get; set; }

    public string? severity { get; set; }

    public string payload { get; set; } = null!;
}
