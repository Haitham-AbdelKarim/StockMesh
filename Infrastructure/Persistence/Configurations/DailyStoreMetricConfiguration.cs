using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class DailyStoreMetricConfiguration : IEntityTypeConfiguration<DailyStoreMetric>
{
    public void Configure(EntityTypeBuilder<DailyStoreMetric> builder)
    {
        builder.HasKey(m => m.Id);

        builder.Property(m => m.StoreId).IsRequired();

        builder.Property(m => m.Date).IsRequired();

        builder.Property(m => m.SalesRevenue).HasColumnType("decimal(18,2)");

        builder.Property(m => m.TransfersOutRevenue).HasColumnType("decimal(18,2)");

        builder.Property(m => m.CostOfGoodsSold).HasColumnType("decimal(18,2)");

        builder.Property(m => m.StockPurchases).HasColumnType("decimal(18,2)");

        builder.Property(m => m.ExpenseTotal).HasColumnType("decimal(18,2)");

        builder.Property(m => m.NetProfit).HasColumnType("decimal(18,2)");

        builder.Property(m => m.RowVersion)
            .IsRowVersion()
            .IsRequired();

        builder.HasIndex(m => new { m.StoreId, m.Date })
            .IsUnique()
            .HasDatabaseName("IX_DailyStoreMetrics_StoreId_Date");

        builder.HasOne<Store>()
            .WithMany()
            .HasForeignKey(m => m.StoreId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}