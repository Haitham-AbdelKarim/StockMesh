namespace Application.Abstractions.Services;

public interface IDailyMetricsMaterializer
{
    Task RecomputeDayAsync(
        IReadOnlyCollection<Guid> storeIds,
        DateTime date,
        CancellationToken cancellationToken = default);
}