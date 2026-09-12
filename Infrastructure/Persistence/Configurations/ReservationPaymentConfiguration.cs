using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class ReservationPaymentConfiguration : IEntityTypeConfiguration<ReservationPayment>
{
    public void Configure(EntityTypeBuilder<ReservationPayment> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.ReservationId).IsRequired();

        builder.Property(p => p.StripeSessionId)
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(p => p.DestinationAccountId)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(p => p.Amount).HasColumnType("decimal(18,2)");

        builder.Property(p => p.Currency)
            .HasMaxLength(8)
            .IsRequired();

        builder.Property(p => p.CheckoutUrl).HasMaxLength(1000);

        builder.Property(p => p.Status)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(p => p.RowVersion)
            .IsRowVersion();

        builder.HasIndex(p => p.ReservationId)
            .IsUnique()
            .HasDatabaseName("IX_ReservationPayments_ReservationId");

        builder.HasOne<StockReservation>()
            .WithMany()
            .HasForeignKey(p => p.ReservationId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}