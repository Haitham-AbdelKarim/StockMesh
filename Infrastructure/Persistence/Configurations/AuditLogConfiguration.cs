using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.HasKey(a => a.Id);

        builder.Property(a => a.EntityType).IsRequired().HasMaxLength(100);

        builder.Property(a => a.EntityId).IsRequired();

        builder.Property(a => a.Action).IsRequired().HasMaxLength(100);

        builder.Property(a => a.Metadata);

        builder.Property(a => a.CreatedAt).IsRequired();

        builder.HasIndex(a => new { a.EntityType, a.EntityId })
            .HasDatabaseName("IX_AuditLogs_Entity");

        builder.HasIndex(a => a.CreatedAt)
            .HasDatabaseName("IX_AuditLogs_CreatedAt");
    }
}