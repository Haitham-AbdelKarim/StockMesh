using FluentAssertions;
using Infrastructure.ExternalServices;
using Infrastructure.Payments;

namespace StockMesh.Infrastructure.UnitTests.ExternalServices;

public class StripeWebhookVerifierTests
{
    [Fact]
    public void ExtractReservationId_WithSessionMetadata_ReturnsIt()
    {
        var reservationId = Guid.NewGuid();

        var result = StripeWebhookVerifier.ExtractReservationId(
            new Dictionary<string, string> { ["reservation_id"] = reservationId.ToString("N") },
            "deadbeefdeadbeefdeadbeefdeadbeef");

        result.Should().Be(reservationId);
    }

    [Fact]
    public void ExtractReservationId_WithoutMetadata_FallsBackToClientReferenceId()
    {
        var reservationId = Guid.NewGuid();

        var result = StripeWebhookVerifier.ExtractReservationId(
            null,
            reservationId.ToString("N"));

        result.Should().Be(reservationId);
    }

    [Fact]
    public void ExtractReservationId_WithEmptyMetadata_FallsBackToClientReferenceId()
    {
        var reservationId = Guid.NewGuid();

        var result = StripeWebhookVerifier.ExtractReservationId(
            new Dictionary<string, string>(),
            reservationId.ToString("N"));

        result.Should().Be(reservationId);
    }

    [Fact]
    public void ExtractReservationId_WithNeither_ReturnsNull()
    {
        var result = StripeWebhookVerifier.ExtractReservationId(null, null);

        result.Should().BeNull();
    }

    [Fact]
    public void ExtractReservationId_WithMalformedValues_ReturnsNull()
    {
        var result = StripeWebhookVerifier.ExtractReservationId(
            new Dictionary<string, string> { ["reservation_id"] = "not-a-guid" },
            "also-not-a-guid");

        result.Should().BeNull();
    }

    [Fact]
    public void Verify_WithNewerApiVersionThanSdk_StillParsesAndExtracts()
    {
        var reservationId = Guid.NewGuid();
        var payload = """
            {
              "id": "evt_test_version_drift",
              "object": "event",
              "api_version": "2999-01-01.future",
              "created": 1789224974,
              "type": "checkout.session.completed",
              "data": {
                "object": {
                  "id": "cs_test_123",
                  "object": "checkout.session",
                  "client_reference_id": "RESERVATIONID",
                  "metadata": {}
                }
              }
            }
            """.Replace("RESERVATIONID", reservationId.ToString("N"));

        var secret = "whsec_test_secret";
        var signature = Stripe.EventUtility.GenerateSignatureHeader(payload, secret, DateTimeOffset.UtcNow.ToUnixTimeSeconds());
        var verifier = new StripeWebhookVerifier(
            new StripeSettings { SecretKey = "sk_test_dummy", WebhookSecret = secret });

        var result = verifier.Verify(payload, signature);

        result.EventId.Should().Be("evt_test_version_drift");
        result.EventType.Should().Be("checkout.session.completed");
        result.ReservationId.Should().Be(reservationId);
        result.SessionId.Should().Be("cs_test_123");
    }
}