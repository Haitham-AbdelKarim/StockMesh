using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;

namespace StockMesh.Api.IntegrationTests;

public class ExpensesAndDashboardEndpointSmokeTests : IClassFixture<TestApiFactory>
{
    private readonly TestApiFactory _factory;

    public ExpensesAndDashboardEndpointSmokeTests(TestApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task RecordExpense_WithoutToken_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/v1/expenses",
            new { category = "Rent", amount = 100m, incurredAt = DateTime.UtcNow });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetExpenses_WithoutToken_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/v1/expenses");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task RecordExpense_WithEmptyCategory_ReturnsUnprocessableEntity()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", TestAuth.CreateAccessToken(Guid.NewGuid()));

        var response = await client.PostAsJsonAsync(
            "/api/v1/expenses",
            new { category = "", amount = 100m, incurredAt = DateTime.UtcNow });

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task RecordExpense_WithNonPositiveAmount_ReturnsUnprocessableEntity()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", TestAuth.CreateAccessToken(Guid.NewGuid()));

        var response = await client.PostAsJsonAsync(
            "/api/v1/expenses",
            new { category = "Rent", amount = 0m, incurredAt = DateTime.UtcNow });

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task GetExpenses_WithOutOfRangePaging_ReturnsUnprocessableEntity()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", TestAuth.CreateAccessToken(Guid.NewGuid()));

        var response = await client.GetAsync("/api/v1/expenses?page=0&pageSize=0");

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task Dashboard_WithoutToken_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var summary = await client.GetAsync("/api/v1/dashboard/summary");
        var trend = await client.GetAsync("/api/v1/dashboard/sales-trend");
        var topSellers = await client.GetAsync("/api/v1/dashboard/top-sellers");
        var slowMovers = await client.GetAsync("/api/v1/dashboard/slow-movers");
        var network = await client.GetAsync("/api/v1/dashboard/network");
        var lowStock = await client.GetAsync("/api/v1/dashboard/low-stock");

        summary.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        trend.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        topSellers.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        slowMovers.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        network.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        lowStock.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Dashboard_WithInvalidQueryParameters_ReturnsUnprocessableEntity()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", TestAuth.CreateAccessToken(Guid.NewGuid()));

        var trend = await client.GetAsync("/api/v1/dashboard/sales-trend?days=0");
        var topSellers = await client.GetAsync("/api/v1/dashboard/top-sellers?topN=0");
        var slowMovers = await client.GetAsync("/api/v1/dashboard/slow-movers?days=6");
        var network = await client.GetAsync("/api/v1/dashboard/network?days=0");

        trend.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        topSellers.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        slowMovers.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        network.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }
}