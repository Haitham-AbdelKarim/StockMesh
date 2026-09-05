using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;

namespace StockMesh.Api.IntegrationTests;

public class CatalogEndpointSmokeTests : IClassFixture<TestApiFactory>
{
    private readonly TestApiFactory _factory;

    public CatalogEndpointSmokeTests(TestApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Products_WithoutToken_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/v1/products");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task StoreProfile_WithoutToken_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/v1/stores/me");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UpdateStoreProfile_WithInvalidBody_ReturnsUnprocessableEntity()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", TestAuth.CreateAccessToken(Guid.NewGuid()));

        var response = await client.PatchAsJsonAsync(
            "/api/v1/stores/me",
            new { name = "", latitude = 30.05, longitude = 31.25, maxSearchRadiusKm = 30 });

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task UpdateStoreProfile_WithOutOfRangeCoordinates_ReturnsUnprocessableEntity()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", TestAuth.CreateAccessToken(Guid.NewGuid()));

        var response = await client.PatchAsJsonAsync(
            "/api/v1/stores/me",
            new { name = "Valid Name", latitude = 91.0, longitude = 31.25, maxSearchRadiusKm = 30 });

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }
}