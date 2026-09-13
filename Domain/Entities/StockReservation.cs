using Domain.Common;
using Domain.Enums;
using Domain.Exceptions;

namespace Domain.Entities;

public class StockReservation : BaseEntity
{
    public Guid BatchId { get; private set; }

    public Guid RequestingStoreId { get; private set; }

    public Guid OwningStoreId { get; private set; }

    public int Quantity { get; private set; }

    public decimal UnitPrice { get; private set; }

    public decimal? DistanceKm { get; private set; }

    public DateTime? DeliveryEta { get; private set; }

    public ReservationStatus Status { get; private set; }

    public DateTime ResolvedAt { get; private set; }

    public DateTime HoldExpiresAt { get; private set; }

    private StockReservation()
    {
    }

    public StockReservation(
        Guid batchId,
        Guid requestingStoreId,
        Guid owningStoreId,
        int quantity,
        decimal unitPrice,
        decimal? distanceKm = null,
        DateTime? deliveryEta = null,
        int holdMinutes = 15,
        DateTime? holdExpiresAt = null)
    {
        BatchId = batchId;
        RequestingStoreId = requestingStoreId;
        OwningStoreId = owningStoreId;
        Quantity = quantity;
        UnitPrice = unitPrice;
        DistanceKm = distanceKm;
        DeliveryEta = deliveryEta;
        Status = ReservationStatus.Pending;
        HoldExpiresAt = holdExpiresAt ?? DateTime.UtcNow.AddMinutes(holdMinutes);
    }

    public void Resolve(ReservationStatus outcome)
    {
        var allowed = (Status, outcome) switch
        {
            (ReservationStatus.Pending, ReservationStatus.Accepted) => true,
            (ReservationStatus.Pending, ReservationStatus.Cancelled) => true,
            (ReservationStatus.Accepted, ReservationStatus.Success) => true,
            (ReservationStatus.Accepted, ReservationStatus.Cancelled) => true,
            _ => false,
        };

        if (!allowed)
        {
            throw new InvalidReservationTransitionException(
                $"Cannot transition reservation from {Status} to {outcome} — only Pending → Accepted/Cancelled and Accepted → Success/Cancelled are allowed.");
        }

        Status = outcome;
        ResolvedAt = DateTime.UtcNow;
    }
}