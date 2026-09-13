using Domain.Entities;
using Infrastructure.Identity;
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

        builder.HasIndex(m => m.BatchId)
            .HasDatabaseName("IX_StockMovements_BatchId");

        builder.HasIndex(m => m.StoreId)
            .HasDatabaseName("IX_StockMovements_StoreId");

        builder.HasIndex(m => new { m.StoreId, m.OccurredAt })
            .HasDatabaseName("IX_StockMovements_StoreId_OccurredAt");

        builder.HasIndex(m => m.RelatedStoreId)
            .HasDatabaseName("IX_StockMovements_RelatedStoreId");

        builder.HasOne<Store>()
            .WithMany()
            .HasForeignKey(m => m.StoreId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<InventoryBatch>()
            .WithMany()
            .HasForeignKey(m => m.BatchId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Store>()
            .WithMany()
            .HasForeignKey(m => m.RelatedStoreId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<StoreUser>()
            .WithMany()
            .HasForeignKey(m => m.StaffUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}