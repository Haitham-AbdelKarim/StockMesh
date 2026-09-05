using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Name)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(p => p.VerticalCategory)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(p => p.Brand)
            .HasMaxLength(100);

        builder.Property(p => p.Barcode)
            .HasMaxLength(100);
    }
}