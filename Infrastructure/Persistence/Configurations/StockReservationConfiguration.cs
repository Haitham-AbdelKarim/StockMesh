using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class StockReservationConfiguration : IEntityTypeConfiguration<StockReservation>
{
    public void Configure(EntityTypeBuilder<StockReservation> builder)
    {
        builder.HasKey(r => r.Id);

        builder.Property(r => r.BatchId).IsRequired();

        builder.Property(r => r.RequestingStoreId).IsRequired();

        builder.Property(r => r.OwningStoreId).IsRequired();

        builder.Property(r => r.UnitPrice).HasColumnType("decimal(18,2)");

        builder.Property(r => r.DistanceKm).HasColumnType("decimal(8,2)");

        builder.HasIndex(r => r.RequestingStoreId)
            .HasDatabaseName("IX_StockReservations_RequestingStoreId");

        builder.HasIndex(r => r.OwningStoreId)
            .HasDatabaseName("IX_StockReservations_OwningStoreId");

        builder.HasIndex(r => new { r.BatchId, r.Status })
            .HasDatabaseName("IX_StockReservations_BatchId_Status");

        builder.HasIndex(r => new { r.RequestingStoreId, r.CreatedAt })
            .HasDatabaseName("IX_StockReservations_RequestingStoreId_CreatedAt");

        builder.HasIndex(r => new { r.OwningStoreId, r.CreatedAt })
            .HasDatabaseName("IX_StockReservations_OwningStoreId_CreatedAt");

        builder.HasIndex(r => r.Status)
            .HasDatabaseName("IX_StockReservations_Status");

        builder.HasOne<InventoryBatch>()
            .WithMany()
            .HasForeignKey(r => r.BatchId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Store>()
            .WithMany()
            .HasForeignKey(r => r.RequestingStoreId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Store>()
            .WithMany()
            .HasForeignKey(r => r.OwningStoreId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}