namespace Application.DTOs.Payments;

public sealed record ConnectOnboardingResponse(
    string AccountId,
    string OnboardingUrl);

public sealed record ConnectStatusResponse(
    string? AccountId,
    bool PayoutsEnabled);

public sealed record CheckoutSessionResponse(
    string SessionId,
    string CheckoutUrl);

public sealed record ReservationPaymentResponse(
    Guid ReservationId,
    string Status,
    decimal Amount,
    string Currency,
    string? CheckoutUrl);