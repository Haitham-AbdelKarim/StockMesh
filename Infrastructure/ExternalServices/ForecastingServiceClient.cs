using System.Net.Http.Json;
using System.Text.Json;
using Application.Abstractions.Services;
using Application.DTOs.Forecasting;
using Microsoft.Extensions.Logging;

namespace Infrastructure.ExternalServices;

public sealed class ForecastingServiceClient : IForecastingClient
{
    private static readonly JsonSerializerOptions RequestJson = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _http;
    private readonly ILogger<ForecastingServiceClient> _logger;

    public ForecastingServiceClient(
        HttpClient http,
        ILogger<ForecastingServiceClient> logger)
    {
        _http = http;
        _logger = logger;
    }

    public async Task<ForecastResponse?> ForecastAsync(
        IReadOnlyList<DailyPoint> series,
        int periodsAhead,
        CancellationToken cancellationToken = default)
    {
        var payload = new
        {
            series = series.Select(point => new
            {
                date = point.Date.ToString("yyyy-MM-dd"),
                value = point.Value
            }),
            periods_ahead = periodsAhead
        };

        try
        {
            using var response = await _http.PostAsJsonAsync(
                "forecast",
                payload,
                RequestJson,
                cancellationToken);

            response.EnsureSuccessStatusCode();

            return await response.Content.ReadFromJsonAsync<ForecastResponse>(
                cancellationToken: cancellationToken);
        }
        catch (Exception ex) when (ex is HttpRequestException
            or TaskCanceledException
            or JsonException
            or InvalidOperationException)
        {
            _logger.LogWarning(ex, "Forecasting service call failed; the caller should skip this product.");

            return null;
        }
    }
}