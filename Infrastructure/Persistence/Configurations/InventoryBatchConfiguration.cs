using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class InventoryBatchConfiguration : IEntityTypeConfiguration<InventoryBatch>
{
    public void Configure(EntityTypeBuilder<InventoryBatch> builder)
    {
        builder.HasKey(b => b.Id);

        builder.Property(b => b.StoreId).IsRequired();

        builder.Property(b => b.ProductId).IsRequired();

        builder.Property(b => b.UnitCost).HasColumnType("decimal(18,2)");

        builder.Property(b => b.UnitSalePrice).HasColumnType("decimal(18,2)");

        builder.Property(b => b.RowVersion)
            .IsRowVersion()
            .IsRequired();

        builder.HasIndex(b => new { b.StoreId, b.ProductId })
            .HasDatabaseName("IX_InventoryBatches_StoreId_ProductId");

        builder.Ignore(b => b.IsShared);
    }
}