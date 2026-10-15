using EdaDemo.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace OrderService.Data;

public sealed class ProcessedEventConfiguration : IEntityTypeConfiguration<ProcessedEvent>
{
    public void Configure(EntityTypeBuilder<ProcessedEvent> builder)
    {
        builder.ToTable("ProcessedEvents");
        builder.HasKey(x => new { x.EventId, x.ConsumerName });
        builder.Property(x => x.ConsumerName).HasMaxLength(200).IsRequired();
    }
}
