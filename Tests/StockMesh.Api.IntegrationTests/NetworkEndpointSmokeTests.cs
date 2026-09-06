using System.Net;
using System.Net.Http.Headers;
using FluentAssertions;

namespace StockMesh.Api.IntegrationTests;

public class NetworkEndpointSmokeTests : IClassFixture<TestApiFactory>
{
    private readonly TestApiFactory _factory;

    public NetworkEndpointSmokeTests(TestApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetListings_WithoutToken_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/v1/network/listings");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetListings_WithInvalidCategory_ReturnsUnprocessableEntity()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", TestAuth.CreateAccessToken(Guid.NewGuid()));

        var response = await client.GetAsync("/api/v1/network/listings?category=999");

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task GetListings_WithInvalidMaxDistance_ReturnsUnprocessableEntity()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", TestAuth.CreateAccessToken(Guid.NewGuid()));

        var response = await client.GetAsync("/api/v1/network/listings?maxDistanceKm=-5");

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }
}