using Application.Abstractions.Services;

namespace Application.Abstractions.Services;

public interface IStripeWebhookVerifier
{
    StripePaymentEvent Verify(string payload, string signatureHeader);
}