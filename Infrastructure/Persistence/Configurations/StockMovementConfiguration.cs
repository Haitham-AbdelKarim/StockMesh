using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class StockMovementConfiguration : IEntityTypeConfiguration<StockMovement>
{
    public void Configure(EntityTypeBuilder<StockMovement> builder)
    {
        builder.HasKey(m => m.Id);

        builder.Property(m => m.StoreId).IsRequired();

        builder.Property(m => m.BatchId).IsRequired();

        builder.Property(m => m.MovementType).IsRequired();

        builder.Property(m => m.UnitPrice).HasColumnType("decimal(18,2)");

        builder.Property(m => m.UnitCost).HasColumnType("decimal(18,2)");
    }
}