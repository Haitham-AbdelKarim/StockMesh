using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using FluentAssertions;

namespace StockMesh.Api.IntegrationTests;

public class PaymentsEndpointSmokeTests : IClassFixture<TestApiFactory>
{
    private readonly TestApiFactory _factory;

    public PaymentsEndpointSmokeTests(TestApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Onboard_WithoutToken_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsync("/api/v1/payments/connect/onboard", null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ConnectStatus_WithoutToken_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/v1/payments/connect/status");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Checkout_WithoutToken_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsync(
            $"/api/v1/reservations/{Guid.NewGuid()}/checkout", null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Webhook_WithoutSignature_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsync(
            "/api/v1/payments/webhook",
            new StringContent("{}", Encoding.UTF8, "application/json"));

        // Anonymous by design (Stripe has no JWT); invalid signature is 400, not 401.
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Webhook_WithGarbageSignature_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/payments/webhook")
        {
            Content = new StringContent("{}", Encoding.UTF8, "application/json"),
        };
        request.Headers.Add("Stripe-Signature", "t=123,v1=deadbeef");

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Resolve_WithSuccessOutcome_ReturnsUnprocessableEntity()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", TestAuth.CreateAccessToken(Guid.NewGuid()));

        // Success is system-only (set by payment confirmation); the API accepts
        // only Accepted or Cancelled outcomes.
        var response = await client.PatchAsJsonAsync(
            $"/api/v1/reservations/{Guid.NewGuid()}",
            new { outcome = "Success" });

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }
}