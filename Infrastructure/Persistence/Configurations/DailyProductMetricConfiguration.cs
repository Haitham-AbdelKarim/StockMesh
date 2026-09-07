using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class DailyProductMetricConfiguration : IEntityTypeConfiguration<DailyProductMetric>
{
    public void Configure(EntityTypeBuilder<DailyProductMetric> builder)
    {
        builder.HasKey(m => m.Id);

        builder.Property(m => m.StoreId).IsRequired();

        builder.Property(m => m.ProductId).IsRequired();

        builder.Property(m => m.Date).IsRequired();

        builder.Property(m => m.SalesRevenue).HasColumnType("decimal(18,2)");

        builder.Property(m => m.SalesCost).HasColumnType("decimal(18,2)");

        builder.Property(m => m.TransfersOutRevenue).HasColumnType("decimal(18,2)");

        builder.Property(m => m.RowVersion)
            .IsRowVersion()
            .IsRequired();

        builder.HasIndex(m => new { m.StoreId, m.ProductId, m.Date })
            .IsUnique()
            .HasDatabaseName("IX_DailyProductMetrics_StoreId_ProductId_Date");

        builder.HasIndex(m => new { m.StoreId, m.Date })
            .HasDatabaseName("IX_DailyProductMetrics_StoreId_Date");

        builder.HasOne<Store>()
            .WithMany()
            .HasForeignKey(m => m.StoreId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Product>()
            .WithMany()
            .HasForeignKey(m => m.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}