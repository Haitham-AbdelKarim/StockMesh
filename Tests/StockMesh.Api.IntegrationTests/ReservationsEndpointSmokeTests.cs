using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;

namespace StockMesh.Api.IntegrationTests;

public class ReservationsEndpointSmokeTests : IClassFixture<TestApiFactory>
{
    private readonly TestApiFactory _factory;

    public ReservationsEndpointSmokeTests(TestApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Reserve_WithoutToken_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/v1/reservations",
            new { batchId = Guid.NewGuid(), quantity = 1 });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Resolve_WithoutToken_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.PatchAsJsonAsync(
            $"/api/v1/reservations/{Guid.NewGuid()}",
            new { outcome = "Success" });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Reserve_WithZeroQuantity_ReturnsUnprocessableEntity()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", TestAuth.CreateAccessToken(Guid.NewGuid()));

        var response = await client.PostAsJsonAsync(
            "/api/v1/reservations",
            new { batchId = Guid.NewGuid(), quantity = 0 });

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task Resolve_WithPendingOutcome_ReturnsUnprocessableEntity()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", TestAuth.CreateAccessToken(Guid.NewGuid()));

        var response = await client.PatchAsJsonAsync(
            $"/api/v1/reservations/{Guid.NewGuid()}",
            new { outcome = "Pending" });

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task Resolve_WithInvalidOutcomeValue_ReturnsUnprocessableEntity()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", TestAuth.CreateAccessToken(Guid.NewGuid()));

        var response = await client.PatchAsJsonAsync(
            $"/api/v1/reservations/{Guid.NewGuid()}",
            new { outcome = "Arriving" });

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }
}