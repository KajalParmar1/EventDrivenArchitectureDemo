using EdaDemo.Messaging;
using Microsoft.EntityFrameworkCore;

namespace NotificationService.Data;

public sealed class NotificationDbContext : DbContext
{
    public NotificationDbContext(DbContextOptions<NotificationDbContext> options)
        : base(options)
    {
    }

    public DbSet<ProcessedEvent> ProcessedEvents => Set<ProcessedEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ProcessedEvent>(builder =>
        {
            builder.ToTable("ProcessedEvents");
            builder.HasKey(x => new { x.EventId, x.ConsumerName });
            builder.Property(x => x.ConsumerName).HasMaxLength(200).IsRequired();
        });
    }
}
