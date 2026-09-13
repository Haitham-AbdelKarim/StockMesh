using Application.Abstractions.Repositories;
using Application.Abstractions.Services;
using Application.DTOs.Payments;
using Application.Exceptions;
using Domain.Entities;

namespace StockMesh.Application.UnitTests.Fakes;

internal sealed class FakeReservationPaymentRepository : IReservationPaymentRepository
{
    private readonly List<ReservationPayment> _payments;

    public FakeReservationPaymentRepository(params ReservationPayment[] payments)
    {
        _payments = payments.ToList();
    }

    public IReadOnlyList<ReservationPayment> All => _payments;

    public Exception? SaveException { get; set; }

    public Task<ReservationPayment?> GetByReservationIdAsync(
        Guid reservationId,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_payments.FirstOrDefault(p => p.ReservationId == reservationId));
    }

    public Task<IReadOnlyDictionary<Guid, ReservationPayment>> GetByReservationIdsAsync(
        IReadOnlyCollection<Guid> reservationIds,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyDictionary<Guid, ReservationPayment> result = _payments
            .Where(p => reservationIds.Contains(p.ReservationId))
            .ToDictionary(p => p.ReservationId);

        return Task.FromResult(result);
    }

    public Task<Guid> AddAsync(
        ReservationPayment payment,
        CancellationToken cancellationToken = default)
    {
        _payments.Add(payment);

        return Task.FromResult(payment.Id);
    }

    public void Update(ReservationPayment payment)
    {
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        if (SaveException is not null)
        {
            throw SaveException;
        }

        return Task.FromResult(0);
    }
}

internal sealed class FakeProcessedStripeEventRepository : IProcessedStripeEventRepository
{
    private readonly List<ProcessedStripeEvent> _events = new();

    public IReadOnlyList<ProcessedStripeEvent> All => _events;

    public bool ThrowOnSave { get; set; }

    public Task<bool> ExistsAsync(string eventId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_events.Any(e => e.EventId == eventId));
    }

    public Task<Guid> AddAsync(
        ProcessedStripeEvent stripeEvent,
        CancellationToken cancellationToken = default)
    {
        _events.Add(stripeEvent);

        return Task.FromResult(stripeEvent.Id);
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        if (ThrowOnSave)
        {
            throw new Microsoft.EntityFrameworkCore.DbUpdateException(
                "Unique index violation on EventId.");
        }

        return Task.FromResult(0);
    }
}

internal sealed class FakePaymentService : IPaymentService
{
    public string DefaultCurrency => "usd";

    public Func<string?, string, ConnectOnboardingResponse> OnboardFactory { get; set; } =
        (existing, email) => new ConnectOnboardingResponse(
            existing ?? "acct_test_123",
            "https://connect.stripe.com/setup/test");

    public Func<string, ConnectStatusResponse> StatusFactory { get; set; } =
        accountId => new ConnectStatusResponse(accountId, true);

    public Func<string, Guid, decimal, CheckoutSessionResponse> SessionFactory { get; set; } =
        (destination, reservationId, amount) => new CheckoutSessionResponse(
            "cs_test_123",
            "https://checkout.stripe.com/pay/test");

    public Exception? ThrowOnSession { get; set; }

    public int SessionCalls { get; private set; }

    public string? LastDestination { get; private set; }

    public Task<ConnectOnboardingResponse> CreateOnboardingLinkAsync(
        string? existingAccountId,
        string storeEmail,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(OnboardFactory(existingAccountId, storeEmail));
    }

    public Task<ConnectStatusResponse> GetAccountStatusAsync(
        string accountId,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(StatusFactory(accountId));
    }

    public Task<CheckoutSessionResponse> CreateCheckoutSessionAsync(
        string destinationAccountId,
        Guid reservationId,
        decimal amount,
        string currency,
        CancellationToken cancellationToken = default)
    {
        SessionCalls++;
        LastDestination = destinationAccountId;

        if (ThrowOnSession is not null)
        {
            throw ThrowOnSession;
        }

        return Task.FromResult(SessionFactory(destinationAccountId, reservationId, amount));
    }
}

internal sealed class FakeStripeWebhookVerifier : IStripeWebhookVerifier
{
    public Func<string, string, StripePaymentEvent> VerifyFactory { get; set; } =
        (payload, signature) => throw new InvalidWebhookSignatureException("Invalid.");

    public StripePaymentEvent Verify(string payload, string signatureHeader)
    {
        return VerifyFactory(payload, signatureHeader);
    }
}