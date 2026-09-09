using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class DailyMarketSignalConfiguration : IEntityTypeConfiguration<DailyMarketSignal>
{
    public void Configure(EntityTypeBuilder<DailyMarketSignal> builder)
    {
        builder.HasKey(s => s.Id);

        builder.Property(s => s.ProductId).IsRequired();

        builder.Property(s => s.VerticalCategory)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(s => s.Date).IsRequired();

        builder.HasIndex(s => new { s.ProductId, s.VerticalCategory, s.Date })
            .IsUnique()
            .HasDatabaseName("IX_DailyMarketSignals_Product_Vertical_Date");

        builder.HasOne<Product>()
            .WithMany()
            .HasForeignKey(s => s.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}