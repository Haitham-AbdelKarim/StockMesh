using Application.DTOs.Payments;

namespace Application.Abstractions.Services;

public interface IPaymentService
{
    string DefaultCurrency { get; }

    Task<ConnectOnboardingResponse> CreateOnboardingLinkAsync(
        string? existingAccountId,
        string storeEmail,
        CancellationToken cancellationToken = default);

    Task<ConnectStatusResponse> GetAccountStatusAsync(
        string accountId,
        CancellationToken cancellationToken = default);

    Task<CheckoutSessionResponse> CreateCheckoutSessionAsync(
        string destinationAccountId,
        Guid reservationId,
        decimal amount,
        string currency,
        CancellationToken cancellationToken = default);
}