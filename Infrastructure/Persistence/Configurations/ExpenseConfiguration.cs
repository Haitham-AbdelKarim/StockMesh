using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class ExpenseConfiguration : IEntityTypeConfiguration<Expense>
{
    public void Configure(EntityTypeBuilder<Expense> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.StoreId).IsRequired();

        builder.Property(e => e.Category)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(e => e.Amount).HasColumnType("decimal(18,2)");

        builder.Property(e => e.IncurredAt).IsRequired();

        builder.HasIndex(e => e.StoreId)
            .HasDatabaseName("IX_Expenses_StoreId");

        builder.HasIndex(e => new { e.StoreId, e.IncurredAt })
            .HasDatabaseName("IX_Expenses_StoreId_IncurredAt");

        builder.HasOne<Store>()
            .WithMany()
            .HasForeignKey(e => e.StoreId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}