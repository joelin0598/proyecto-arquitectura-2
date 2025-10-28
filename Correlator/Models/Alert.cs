using System;
using System.Collections.Generic;

namespace Correlator.Models;

public partial class alert
{
    public Guid alert_id { get; set; }

    public Guid? correlation_id { get; set; }

    public string type { get; set; } = null!;

    public decimal? score { get; set; }

    public string? zone { get; set; }

    public DateTime? window_start { get; set; }

    public DateTime? window_end { get; set; }

    public string? evidence { get; set; }

    public DateTime created_at { get; set; }
}
