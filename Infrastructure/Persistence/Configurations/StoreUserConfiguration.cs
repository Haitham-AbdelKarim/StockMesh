using Domain.Entities;
using Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class StoreUserConfiguration : IEntityTypeConfiguration<StoreUser>
{
    public void Configure(EntityTypeBuilder<StoreUser> builder)
    {
        builder.ToTable("StoreUsers");

        builder.Property(u => u.StoreId).IsRequired();

        builder.Property(u => u.VerticalCategory)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(u => u.RefreshTokenHash)
            .HasMaxLength(64);

        builder.HasIndex(u => u.RefreshTokenHash)
            .HasDatabaseName("IX_StoreUsers_RefreshTokenHash");

        builder.HasIndex(u => u.StoreId)
            .HasDatabaseName("IX_StoreUsers_StoreId");

        builder.HasOne<Store>()
            .WithMany()
            .HasForeignKey(u => u.StoreId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}