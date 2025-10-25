using System;
using System.Collections.Generic;
using Correlator.Models;
using Microsoft.EntityFrameworkCore;

namespace Correlator.Data;

public partial class AppDbContext : DbContext
{
    public AppDbContext()
    {
    }

    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<_event> events { get; set; }

    public virtual DbSet<alert> alerts { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            optionsBuilder.UseNpgsql("Host=db-postgres;Port=5432;Database=alertsdb;Username=appuser;Password=appsecret;");
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<_event>(entity =>
        {
            entity.HasKey(e => e.event_id).HasName("events_pkey");

            entity.HasIndex(e => e.partition_key, "idx_events_pkey");

            entity.HasIndex(e => e.ts_utc, "idx_events_ts");

            entity.HasIndex(e => e.event_type, "idx_events_type");

            entity.HasIndex(e => e.zone, "idx_events_zone");

            entity.Property(e => e.event_id).ValueGeneratedNever();
            entity.Property(e => e.payload).HasColumnType("jsonb");
            entity.Property(e => e.geo_lon).HasColumnName("geo_long");

        });

        modelBuilder.Entity<alert>(entity =>
        {
            entity.HasKey(e => e.alert_id).HasName("alerts_pkey");

            entity.HasIndex(e => e.created_at, "idx_alerts_ts");

            entity.HasIndex(e => e.zone, "idx_alerts_zone");

            entity.Property(e => e.alert_id).ValueGeneratedNever();
            entity.Property(e => e.created_at).HasDefaultValueSql("now()");
            entity.Property(e => e.evidence).HasColumnType("jsonb");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
