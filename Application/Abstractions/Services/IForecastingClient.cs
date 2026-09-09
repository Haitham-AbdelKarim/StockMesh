using Application.DTOs.Forecasting;

namespace Application.Abstractions.Services;

public sealed record DailyPoint(DateOnly Date, double Value);

public interface IForecastingClient
{
    Task<ForecastResponse?> ForecastAsync(
        IReadOnlyList<DailyPoint> series,
        int periodsAhead,
        CancellationToken cancellationToken = default);
}