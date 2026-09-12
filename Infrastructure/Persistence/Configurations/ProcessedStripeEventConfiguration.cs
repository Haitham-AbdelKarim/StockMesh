using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class ProcessedStripeEventConfiguration : IEntityTypeConfiguration<ProcessedStripeEvent>
{
    public void Configure(EntityTypeBuilder<ProcessedStripeEvent> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.EventId)
            .HasMaxLength(255)
            .IsRequired();

        builder.HasIndex(e => e.EventId)
            .IsUnique()
            .HasDatabaseName("IX_ProcessedStripeEvents_EventId");
    }
}