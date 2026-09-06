using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;

namespace StockMesh.Api.IntegrationTests;

public class StockEndpointSmokeTests : IClassFixture<TestApiFactory>
{
    private readonly TestApiFactory _factory;

    public StockEndpointSmokeTests(TestApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task RecordSale_WithoutToken_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/v1/sales",
            new { batchId = Guid.NewGuid(), quantity = 1 });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task RecordRestock_WithoutToken_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/v1/restocks",
            new { productId = Guid.NewGuid(), quantity = 5, unitCost = 3m });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetStockMovements_WithoutToken_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/v1/stock-movements");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task RecordSale_WithZeroQuantity_ReturnsUnprocessableEntity()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", TestAuth.CreateAccessToken(Guid.NewGuid()));

        var response = await client.PostAsJsonAsync(
            "/api/v1/sales",
            new { batchId = Guid.NewGuid(), quantity = 0 });

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task RecordRestock_WithNegativeUnitCost_ReturnsUnprocessableEntity()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", TestAuth.CreateAccessToken(Guid.NewGuid()));

        var response = await client.PostAsJsonAsync(
            "/api/v1/restocks",
            new { productId = Guid.NewGuid(), quantity = 5, unitCost = -1m });

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task GetStockMovements_WithOutOfRangePaging_ReturnsUnprocessableEntity()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", TestAuth.CreateAccessToken(Guid.NewGuid()));

        var response = await client.GetAsync("/api/v1/stock-movements?page=0&pageSize=0");

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }
}