using Domain.Common;
using Domain.Enums;

namespace Domain.Entities;

public class ReservationPayment : BaseEntity
{
    public Guid ReservationId { get; private set; }

    public string StripeSessionId { get; private set; } = string.Empty;

    public string DestinationAccountId { get; private set; } = string.Empty;

    public decimal Amount { get; private set; }

    public string Currency { get; private set; } = string.Empty;

    public string? CheckoutUrl { get; private set; }

    public PaymentStatus Status { get; private set; }

    public byte[] RowVersion { get; private set; } = [];

    private ReservationPayment()
    {
    }

    public ReservationPayment(
        Guid reservationId,
        string stripeSessionId,
        string destinationAccountId,
        decimal amount,
        string currency,
        string? checkoutUrl = null)
    {
        if (string.IsNullOrWhiteSpace(stripeSessionId))
        {
            throw new ArgumentException("Stripe session id is required.", nameof(stripeSessionId));
        }

        if (string.IsNullOrWhiteSpace(destinationAccountId))
        {
            throw new ArgumentException("Destination account id is required.", nameof(destinationAccountId));
        }

        if (amount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "Payment amount must be positive.");
        }

        ReservationId = reservationId;
        StripeSessionId = stripeSessionId;
        DestinationAccountId = destinationAccountId;
        Amount = amount;
        Currency = currency;
        CheckoutUrl = checkoutUrl;
        Status = PaymentStatus.Pending;
    }

    public void MarkPaid()
    {
        if (Status != PaymentStatus.Pending)
        {
            throw new InvalidOperationException(
                $"Cannot mark payment {Status} as paid — only Pending payments can be paid.");
        }

        Status = PaymentStatus.Paid;
    }

    public void MarkFailed()
    {
        if (Status != PaymentStatus.Pending)
        {
            throw new InvalidOperationException(
                $"Cannot mark payment {Status} as failed — only Pending payments can fail.");
        }

        Status = PaymentStatus.Failed;
    }

    public void ReopenForRetry(string stripeSessionId, string? checkoutUrl)
    {
        if (Status != PaymentStatus.Failed)
        {
            throw new InvalidOperationException(
                $"Cannot reopen payment {Status} for retry — only Failed payments can be retried.");
        }

        if (string.IsNullOrWhiteSpace(stripeSessionId))
        {
            throw new ArgumentException("Stripe session id is required.", nameof(stripeSessionId));
        }

        StripeSessionId = stripeSessionId;
        CheckoutUrl = checkoutUrl;
        Status = PaymentStatus.Pending;
    }
}