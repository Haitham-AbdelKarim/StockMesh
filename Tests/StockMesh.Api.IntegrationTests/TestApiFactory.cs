using Api;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace StockMesh.Api.IntegrationTests;

public class TestApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting(
            "ConnectionStrings:DefaultConnection",
            "Server=localhost,59999;Database=StockMesh_Tests;User Id=sa;Password=Dummy!Passw0rd;TrustServerCertificate=True;Connect Timeout=1;");

        builder.UseSetting(
            "JwtSettings:Key",
            "8d26a0177d174f9b3084920b7afc9b15c9246c3676403fc618ad2804ad319ccf");
        builder.UseSetting("JwtSettings:Issuer", "StockMesh");
        builder.UseSetting("JwtSettings:Audience", "StockMesh");
        builder.UseSetting("JwtSettings:AccessTokenExpirationMinutes", "15");
        builder.UseSetting("JwtSettings:RefreshTokenExpirationDays", "7");

        // Every required config section must be mirrored here, or the test host
        // fails to build. Values are dummies: smoke tests never call this client.
        builder.UseSetting("Forecasting:BaseUrl", "http://localhost:8000");
        builder.UseSetting("Forecasting:TimeoutSeconds", "30");
    }
}