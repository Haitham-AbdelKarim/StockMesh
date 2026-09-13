using System.Net;
using System.Text.Json;
using FluentAssertions;

namespace StockMesh.Api.IntegrationTests;

public class HealthCheckTests : IClassFixture<TestApiFactory>
{
    private readonly TestApiFactory _factory;

    public HealthCheckTests(TestApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Health_Endpoint_Returns_ServiceUnavailable_WhenDependenciesUnavailable()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/health");

        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);

        var body = await response.Content.ReadAsStringAsync();

        using var doc = JsonDocument.Parse(body);

        doc.RootElement.GetProperty("status").GetString().Should().Be("Degraded");

        var results = doc.RootElement.GetProperty("results");

        results.TryGetProperty("database", out _).Should().BeTrue();
        results.TryGetProperty("redis", out _).Should().BeTrue();
        results.TryGetProperty("forecasting-service", out _).Should().BeTrue();
        results.TryGetProperty("agent-service", out _).Should().BeTrue();
        results.TryGetProperty("stripe", out _).Should().BeTrue();
    }
}