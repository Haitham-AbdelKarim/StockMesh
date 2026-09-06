using Domain.Entities;

namespace Application.DTOs.Reservations;

public static class ReservationMapper
{
    public static ReservationResponse ToResponse(StockReservation reservation)
    {
        return new ReservationResponse(
            reservation.Id,
            reservation.BatchId,
            reservation.RequestingStoreId,
            reservation.OwningStoreId,
            reservation.Quantity,
            reservation.UnitPrice,
            reservation.DistanceKm,
            reservation.DeliveryEta,
            reservation.Status,
            reservation.HoldExpiresAt,
            reservation.ResolvedAt == default ? null : reservation.ResolvedAt);
    }
}