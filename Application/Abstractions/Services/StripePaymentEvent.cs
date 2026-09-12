namespace Application.Abstractions.Services;

public sealed record StripePaymentEvent(
    string EventId,
    string EventType,
    Guid? ReservationId,
    string? SessionId);