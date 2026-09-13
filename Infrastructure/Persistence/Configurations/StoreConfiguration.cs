using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class StoreConfiguration : IEntityTypeConfiguration<Store>
{
    public void Configure(EntityTypeBuilder<Store> builder)
    {
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Name)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(s => s.VerticalCategory)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(s => s.Latitude).IsRequired();

        builder.Property(s => s.Longitude).IsRequired();

        builder.Property(s => s.IsVerified).IsRequired();

        builder.Property(s => s.MaxSearchRadiusKm).IsRequired();

        builder.Property(s => s.StripeConnectAccountId)
            .HasMaxLength(64);

        builder.Property(s => s.PayoutsEnabled).IsRequired();
    }
}