using Application.Abstractions.Services;
using Application.Exceptions;
using Infrastructure.ExternalServices;
using Stripe;

namespace Infrastructure.Payments;

public sealed class StripeWebhookVerifier : IStripeWebhookVerifier
{
    private readonly StripeSettings _settings;

    public StripeWebhookVerifier(StripeSettings settings)
    {
        _settings = settings;
    }

    public StripePaymentEvent Verify(string payload, string signatureHeader)
    {
        Event stripeEvent;

        try
        {
            // Tolerant of API-version drift by design: Stripe bumps versions
            // regularly, and webhooks must not start failing just because the
            // platform moved forward. We read only a version-stable surface
            // (id, type, session metadata/client_reference_id).
            stripeEvent = EventUtility.ConstructEvent(
                payload,
                signatureHeader,
                _settings.WebhookSecret,
                throwOnApiVersionMismatch: false);
        }
        catch (StripeException ex)
        {
            throw new InvalidWebhookSignatureException("Invalid webhook signature.", ex);
        }

        return stripeEvent.Type switch
        {
            "checkout.session.completed" or "checkout.session.expired" =>
                new StripePaymentEvent(
                    stripeEvent.Id,
                    stripeEvent.Type,
                    ExtractReservationId(
                        (stripeEvent.Data.Object as Stripe.Checkout.Session)?.Metadata,
                        (stripeEvent.Data.Object as Stripe.Checkout.Session)?.ClientReferenceId),
                    (stripeEvent.Data.Object as Stripe.Checkout.Session)?.Id),
            "payment_intent.payment_failed" =>
                new StripePaymentEvent(
                    stripeEvent.Id,
                    stripeEvent.Type,
                    ExtractReservationId((stripeEvent.Data.Object as PaymentIntent)?.Metadata, null),
                    null),
            _ => new StripePaymentEvent(stripeEvent.Id, stripeEvent.Type, null, null),
        };
    }

    public static Guid? ExtractReservationId(
        Dictionary<string, string>? metadata,
        string? clientReferenceId)
    {
        if (metadata is not null
            && metadata.TryGetValue("reservation_id", out var raw)
            && Guid.TryParseExact(raw, "N", out var fromMetadata))
        {
            return fromMetadata;
        }

        // Fallback for sessions created without session-level metadata
        // (only PaymentIntent metadata + ClientReferenceId): the reference
        // is still recoverable, which also rescues redelivered events.
        if (!string.IsNullOrWhiteSpace(clientReferenceId)
            && Guid.TryParseExact(clientReferenceId, "N", out var fromReference))
        {
            return fromReference;
        }

        return null;
    }
}