using Domain.Entities;

namespace Application.DTOs.Reservations;

public static class ReservationMapper
{
    public static ReservationResponse ToResponse(
        StockReservation reservation,
        ReservationPaymentState paymentState = ReservationPaymentState.Unpaid)
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
            reservation.ResolvedAt == default ? null : reservation.ResolvedAt,
            paymentState);
    }

    public static ReservationPaymentState ToPaymentState(Domain.Enums.PaymentStatus status)
    {
        return status switch
        {
            Domain.Enums.PaymentStatus.Paid => ReservationPaymentState.Paid,
            Domain.Enums.PaymentStatus.Failed => ReservationPaymentState.Failed,
            _ => ReservationPaymentState.Pending,
        };
    }
}