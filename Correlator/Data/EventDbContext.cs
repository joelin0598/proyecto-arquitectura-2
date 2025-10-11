using Microsoft.EntityFrameworkCore;
using EventProcessor.Models;
using Correlator.Models;

namespace EventProcessor.Data;

public class EventDbContext : DbContext
{
    public EventDbContext(DbContextOptions<EventDbContext> options) : base(options) { }
    public DbSet<EventEntity> Events { get; set; }
    public DbSet<Alert> Alerts { get; set; }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Define Indexes for the 'events' table
        modelBuilder.Entity<EventEntity>()
            .HasIndex(e => e.TsUtc)
            .HasDatabaseName("idx_events_ts");

        modelBuilder.Entity<EventEntity>()
            .HasIndex(e => e.EventType)
            .HasDatabaseName("idx_events_type");

        modelBuilder.Entity<EventEntity>()
            .HasIndex(e => e.Zone)
            .HasDatabaseName("idx_events_zone");

        modelBuilder.Entity<EventEntity>()
            .HasIndex(e => e.PartitionKey)
            .HasDatabaseName("idx_events_pkey");

        // Define Indexes for the 'alerts' table
        modelBuilder.Entity<Alert>()
            .HasIndex(a => a.CreatedAt)
            .HasDatabaseName("idx_alerts_ts");

        modelBuilder.Entity<Alert>()
            .HasIndex(a => a.Zone)
            .HasDatabaseName("idx_alerts_zone");
    }

}