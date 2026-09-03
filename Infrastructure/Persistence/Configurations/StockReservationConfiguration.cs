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
    }
}