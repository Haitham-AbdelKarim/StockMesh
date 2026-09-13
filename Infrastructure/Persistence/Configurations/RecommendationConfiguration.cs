using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class RecommendationConfiguration : IEntityTypeConfiguration<Recommendation>
{
    public void Configure(EntityTypeBuilder<Recommendation> builder)
    {
        builder.HasKey(r => r.Id);

        builder.Property(r => r.StoreId).IsRequired();

        builder.Property(r => r.ProductId).IsRequired();

        builder.Property(r => r.RecommendedAction)
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Property(r => r.ModelVersion)
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(r => r.Reason)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(r => r.GeneratedAt).IsRequired();

        builder.HasIndex(r => new { r.StoreId, r.ProductId, r.BatchId })
            .HasDatabaseName("IX_Recommendations_Store_Product_Batch");

        builder.HasOne<Store>()
            .WithMany()
            .HasForeignKey(r => r.StoreId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Product>()
            .WithMany()
            .HasForeignKey(r => r.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<InventoryBatch>()
            .WithMany()
            .HasForeignKey(r => r.BatchId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}