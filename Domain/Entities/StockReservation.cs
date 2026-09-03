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
        int holdMinutes = 15)
    {
        BatchId = batchId;
        RequestingStoreId = requestingStoreId;
        OwningStoreId = owningStoreId;
        Quantity = quantity;
        UnitPrice = unitPrice;
        DistanceKm = distanceKm;
        DeliveryEta = deliveryEta;
        Status = ReservationStatus.Pending;
        HoldExpiresAt = DateTime.UtcNow.AddMinutes(holdMinutes);
    }

    public void Resolve(ReservationStatus outcome)
    {
        if (Status != ReservationStatus.Pending)
        {
            throw new InvalidReservationTransitionException(
                $"Cannot transition reservation from {Status} to {outcome} — only Pending transitions are allowed.");
        }

        if (outcome != ReservationStatus.Success && outcome != ReservationStatus.Cancelled)
        {
            throw new InvalidReservationTransitionException(
                $"Reservation resolution must be either Success or Cancelled, got {outcome}.");
        }

        Status = outcome;
        ResolvedAt = DateTime.UtcNow;
    }
}