using System.Net.Http;
using System.Net.Http.Headers;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using StackExchange.Redis;

namespace Api.Diagnostics;

/// <summary>
/// Health check that probes the primary SQL database via the app's own
/// <see cref="AppDbContext"/> with a short timeout.
/// </summary>
public sealed class DbAvailabilityHealthCheck : IHealthCheck
{
    private readonly AppDbContext _dbContext;
    private readonly TimeSpan _timeout = TimeSpan.FromSeconds(3);

    public DbAvailabilityHealthCheck(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(_timeout);

            var canConnect = await _dbContext.Database.CanConnectAsync(cts.Token);

            return canConnect
                ? HealthCheckResult.Healthy("Connection succeeded.")
                : HealthCheckResult.Degraded("Cannot connect to the database.");
        }
        catch (OperationCanceledException)
        {
            return HealthCheckResult.Degraded("Timed out.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Degraded($"Unreachable: {ex.Message}");
        }
    }
}

/// <summary>
/// Health check that probes an upstream HTTP service's /health endpoint.
/// Reports <see cref="HealthStatus.Degraded"/> (never Unhealthy) so the
/// liveness of the API itself is not conflated with a dependency being down.
/// </summary>
public sealed class ExternalApiHealthCheck : IHealthCheck
{
    private readonly HttpClient _httpClient;
    private readonly string _path;

    public ExternalApiHealthCheck(HttpClient httpClient, string path)
    {
        _httpClient = httpClient;
        _path = path;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var response = await _httpClient.GetAsync(
                _path,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);

            return response.IsSuccessStatusCode
                ? HealthCheckResult.Healthy($"Responded {(int)response.StatusCode}.")
                : HealthCheckResult.Degraded($"Unhealthy status {(int)response.StatusCode}.");
        }
        catch (OperationCanceledException)
        {
            return HealthCheckResult.Degraded("Timed out.");
        }
        catch (HttpRequestException ex)
        {
            return HealthCheckResult.Degraded($"Unreachable: {ex.Message}");
        }
    }
}

/// <summary>
/// Health check that pings Redis using the same configuration key as
/// <c>RedisReservationLockService</c>. Uses a short timeout and
/// <c>abortConnect=false</c> so an unavailable Redis reports Degraded.
/// </summary>
public sealed class RedisHealthCheck : IHealthCheck
{
    private readonly string _configuration;
    private readonly TimeSpan _timeout;

    public RedisHealthCheck(string configuration, TimeSpan timeout)
    {
        _configuration = configuration;
        _timeout = timeout;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(_timeout);

            var multiplexer = await ConnectionMultiplexer.ConnectAsync(
                _configuration);

            try
            {
                await multiplexer.GetDatabase().PingAsync();
            }
            finally
            {
                multiplexer.Dispose();
            }

            return HealthCheckResult.Healthy("Ping succeeded.");
        }
        catch (OperationCanceledException)
        {
            return HealthCheckResult.Degraded("Timed out.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Degraded($"Unreachable: {ex.Message}");
        }
    }
}

/// <summary>
/// Health check that probes the Stripe API live using the configured test-mode
/// secret key. A rejected key (401) reports Degraded, not Healthy.
/// </summary>
public sealed class StripeHealthCheck : IHealthCheck
{
    private readonly HttpClient _httpClient;

    public StripeHealthCheck(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, "v1/balance");
            using var response = await _httpClient.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);

            return response.IsSuccessStatusCode
                ? HealthCheckResult.Healthy("Api reachable.")
                : HealthCheckResult.Degraded($"Unhealthy status {(int)response.StatusCode}.");
        }
        catch (OperationCanceledException)
        {
            return HealthCheckResult.Degraded("Timed out.");
        }
        catch (HttpRequestException ex)
        {
            return HealthCheckResult.Degraded($"Unreachable: {ex.Message}");
        }
    }

    public static HttpClient CreateClient(string secretKey)
    {
        var client = new HttpClient
        {
            BaseAddress = new Uri("https://api.stripe.com"),
            Timeout = TimeSpan.FromSeconds(5)
        };
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", secretKey);

        return client;
    }
}