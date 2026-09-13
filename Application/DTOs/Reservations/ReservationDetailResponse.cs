using Domain.Enums;

namespace Application.DTOs.Reservations;

public sealed record ReservationDetailResponse(
    Guid Id,
    Guid BatchId,
    Guid ProductId,
    string ProductName,
    Guid RequestingStoreId,
    string RequestingStoreName,
    Guid OwningStoreId,
    string OwningStoreName,
    int Quantity,
    decimal UnitPrice,
    decimal? DistanceKm,
    DateTime? DeliveryEta,
    ReservationStatus Status,
    DateTime HoldExpiresAt,
    DateTime? ResolvedAt,
    ReservationPaymentState PaymentState);