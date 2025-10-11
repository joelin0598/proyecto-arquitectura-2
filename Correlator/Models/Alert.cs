using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace Correlator.Models
{
    [Table("alerts")]
    public class Alert
    {
        [Key]
        [Column("alert_id")]
        public Guid AlertId { get; set; }

        [Column("correlation_id")]
        public Guid? CorrelationId { get; set; }

        [Required]
        [Column("type")]
        public string Type { get; set; }

        [Column("score")]
        public double? Score { get; set; }

        [Column("zone")]
        public string Zone { get; set; }

        [Column("window_start")]
        public DateTimeOffset? WindowStart { get; set; }

        [Column("window_end")]
        public DateTimeOffset? WindowEnd { get; set; }

        [Column("evidence")]
        public string Evidence { get; set; } // You may want to store it as a JSON or string, depending on your requirements.

        [Required]
        [Column("created_at")]
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    }
}