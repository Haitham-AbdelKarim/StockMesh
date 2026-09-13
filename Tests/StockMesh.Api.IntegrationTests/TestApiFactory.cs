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
        builder.UseSetting("Stripe:SecretKey", "sk_test_dummy");
        builder.UseSetting("Stripe:WebhookSecret", "whsec_dummy");
        builder.UseSetting("Stripe:Currency", "usd");
        builder.UseSetting("Stripe:SuccessUrl", "http://localhost:4200/reservations?payment=success");
        builder.UseSetting("Stripe:CancelUrl", "http://localhost:4200/reservations?payment=cancelled");
        builder.UseSetting("Stripe:ConnectRefreshUrl", "http://localhost:4200/settings?connect=refresh");
        builder.UseSetting("Stripe:ConnectReturnUrl", "http://localhost:4200/settings?connect=done");
        builder.UseSetting("Assistant:BaseUrl", "http://localhost:8001");
        builder.UseSetting("Assistant:TimeoutSeconds", "120");
        builder.UseSetting("Assistant:MaxQuestionsPerMinute", "10");
        builder.UseSetting("AgentLog:InternalKey", "test-internal-key");
    }
}