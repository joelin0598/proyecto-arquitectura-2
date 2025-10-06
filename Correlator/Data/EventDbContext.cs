using Microsoft.EntityFrameworkCore;
using EventProcessor.Models;

namespace EventProcessor.Data;

public class EventDbContext : DbContext
{
    public EventDbContext(DbContextOptions<EventDbContext> options) : base(options) { }
    public DbSet<EventEntity> Events { get; set; }
}