using System.Net;
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
    public async Task Health_Endpoint_Returns_Ok()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}