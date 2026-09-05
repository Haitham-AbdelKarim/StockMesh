using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Api;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace StockMesh.Api.IntegrationTests;

public class AuthControllerSmokeTests : IClassFixture<AuthApiFactory>
{
    private readonly AuthApiFactory _factory;

    public AuthControllerSmokeTests(AuthApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task JoinStore_WithoutToken_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/v1/auth/join-store",
            new { email = "staff@store.com", password = "Password123" });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task RegisterStore_WithInvalidPayload_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsync(
            "/api/v1/auth/register-store",
            new StringContent(
                "{}",
                Encoding.UTF8,
                "application/json"));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Refresh_WithMissingToken_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsync(
            "/api/v1/auth/refresh",
            new StringContent(
                JsonSerializer.Serialize(new { }),
                Encoding.UTF8,
                "application/json"));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}

public class AuthApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] =
                    "Server=localhost,59999;Database=StockMesh_Tests;User Id=sa;Password=Dummy!Passw0rd;TrustServerCertificate=True;Connect Timeout=1;"
            });
        });
    }
}