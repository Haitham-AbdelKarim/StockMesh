using Domain.Enums;

namespace Application.DTOs.Reservations;

public sealed record ReservationResponse(
    Guid Id,
    Guid BatchId,
    Guid RequestingStoreId,
    Guid OwningStoreId,
    int Quantity,
    decimal UnitPrice,
    decimal? DistanceKm,
    DateTime? DeliveryEta,
    ReservationStatus Status,
    DateTime HoldExpiresAt,
    DateTime? ResolvedAt);