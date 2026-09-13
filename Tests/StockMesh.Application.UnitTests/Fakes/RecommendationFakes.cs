using Application.Abstractions.Repositories;
using Application.Abstractions.Services;
using Application.DTOs.Forecasting;
using Domain.Entities;

namespace StockMesh.Application.UnitTests.Fakes;

internal sealed class FakeRecommendationRepository : IRecommendationRepository
{
    private readonly List<Recommendation> _rows = new();

    public FakeRecommendationRepository(params Recommendation[] rows)
    {
        _rows.AddRange(rows);
    }

    public IReadOnlyList<Recommendation> All => _rows;

    public Task<IReadOnlyList<Recommendation>> GetForProductAsync(
        Guid storeId,
        Guid productId,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<Recommendation>>(
            _rows.Where(r => r.StoreId == storeId && r.ProductId == productId).ToList());
    }

    public Task<(IReadOnlyList<Recommendation> Items, int TotalCount)> GetForStoreAsync(
        Guid storeId,
        Domain.Enums.RecommendedAction? recommendedAction,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _rows.Where(r => r.StoreId == storeId);
        if (recommendedAction is { } action)
        {
            query = query.Where(r => r.RecommendedAction == action);
        }

        var all = query.OrderByDescending(r => r.GeneratedAt).ToList();
        var items = all.Skip((page - 1) * pageSize).Take(pageSize).ToList();

        return Task.FromResult(((IReadOnlyList<Recommendation>)items, all.Count));
    }

    public Task AddAsync(
        Recommendation recommendation,
        CancellationToken cancellationToken = default)
    {
        _rows.Add(recommendation);

        return Task.CompletedTask;
    }

    public void RemoveRange(
        IEnumerable<Recommendation> recommendations)
    {
        foreach (var recommendation in recommendations.ToList())
        {
            _rows.Remove(recommendation);
        }
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(0);
    }
}

internal sealed class FakeForecastingClient : IForecastingClient
{
    private readonly Func<IReadOnlyList<DailyPoint>, int, ForecastResponse?> _respond;

    public FakeForecastingClient(Func<IReadOnlyList<DailyPoint>, int, ForecastResponse?> respond)
    {
        _respond = respond;
    }

    public List<(IReadOnlyList<DailyPoint> Series, int PeriodsAhead)> Calls { get; } = new();

    public Task<ForecastResponse?> ForecastAsync(
        IReadOnlyList<DailyPoint> series,
        int periodsAhead,
        CancellationToken cancellationToken = default)
    {
        Calls.Add((series, periodsAhead));

        return Task.FromResult(_respond(series, periodsAhead));
    }
}