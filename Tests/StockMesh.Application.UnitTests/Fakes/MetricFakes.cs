using Application.Abstractions.Repositories;
using Application.Abstractions.Services;
using Domain.Entities;
using Domain.Enums;

namespace StockMesh.Application.UnitTests.Fakes;

internal sealed class FakeExpenseRepository : IExpenseRepository
{
    private readonly List<Expense> _expenses;

    public FakeExpenseRepository(params Expense[] expenses)
    {
        _expenses = expenses.ToList();
    }

    public IReadOnlyList<Expense> All => _expenses;

    public Task<Guid> AddAsync(
        Expense expense,
        CancellationToken cancellationToken = default)
    {
        _expenses.Add(expense);

        return Task.FromResult(expense.Id);
    }

    public Task<(IReadOnlyList<Expense> Items, int TotalCount)> GetByStoreAsync(
        Guid storeId,
        string? category,
        DateTime? from,
        DateTime? to,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _expenses.Where(e => e.StoreId == storeId);

        if (!string.IsNullOrWhiteSpace(category))
        {
            query = query.Where(e => e.Category == category);
        }

        if (from is { } fromDate)
        {
            query = query.Where(e => e.IncurredAt >= fromDate);
        }

        if (to is { } toDate)
        {
            query = query.Where(e => e.IncurredAt <= toDate);
        }

        var all = query
            .OrderByDescending(e => e.IncurredAt)
            .ToList();

        var items = all
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return Task.FromResult(((IReadOnlyList<Expense>)items, all.Count));
    }

    public Task<IReadOnlyList<Expense>> GetForStoresAsync(
        IReadOnlyCollection<Guid> storeIds,
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<Expense>>(
            _expenses
                .Where(e => storeIds.Contains(e.StoreId) && e.IncurredAt >= from && e.IncurredAt < to)
                .ToList());
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(0);
    }
}

internal sealed class FakeDailyStoreMetricRepository : IDailyStoreMetricRepository
{
    private readonly Dictionary<(Guid StoreId, DateTime Date), DailyStoreMetric> _metrics;

    public FakeDailyStoreMetricRepository(params DailyStoreMetric[] metrics)
    {
        _metrics = metrics
            .ToDictionary(m => (m.StoreId, m.Date), m => m);
    }

    public IReadOnlyList<DailyStoreMetric> All => _metrics.Values.ToList();

    public Task<DailyStoreMetric?> GetAsync(
        Guid storeId,
        DateTime date,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_metrics.GetValueOrDefault((storeId, date)));
    }

    public Task<Guid> AddAsync(
        DailyStoreMetric metric,
        CancellationToken cancellationToken = default)
    {
        _metrics[(metric.StoreId, metric.Date)] = metric;

        return Task.FromResult(metric.Id);
    }

    public void Update(DailyStoreMetric metric)
    {
        _metrics[(metric.StoreId, metric.Date)] = metric;
    }

    public Task<IReadOnlyList<DailyStoreMetric>> GetRangeAsync(
        Guid storeId,
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<DailyStoreMetric>>(
            _metrics.Values
                .Where(m => m.StoreId == storeId && m.Date >= from && m.Date < to)
                .OrderBy(m => m.Date)
                .ToList());
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(0);
    }
}

internal sealed class FakeDailyProductMetricRepository : IDailyProductMetricRepository
{
    private readonly Dictionary<(Guid StoreId, Guid ProductId, DateTime Date), DailyProductMetric> _metrics;

    public FakeDailyProductMetricRepository(params DailyProductMetric[] metrics)
    {
        _metrics = metrics
            .ToDictionary(m => (m.StoreId, m.ProductId, m.Date), m => m);
    }

    public IReadOnlyList<DailyProductMetric> All => _metrics.Values.ToList();

    public Task<DailyProductMetric?> GetAsync(
        Guid storeId,
        Guid productId,
        DateTime date,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_metrics.GetValueOrDefault((storeId, productId, date)));
    }

    public Task<Guid> AddAsync(
        DailyProductMetric metric,
        CancellationToken cancellationToken = default)
    {
        _metrics[(metric.StoreId, metric.ProductId, metric.Date)] = metric;

        return Task.FromResult(metric.Id);
    }

    public void Update(DailyProductMetric metric)
    {
        _metrics[(metric.StoreId, metric.ProductId, metric.Date)] = metric;
    }

    public Task<IReadOnlyList<DailyProductMetric>> GetRangeAsync(
        Guid storeId,
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<DailyProductMetric>>(
            _metrics.Values
                .Where(m => m.StoreId == storeId && m.Date >= from && m.Date < to)
                .OrderBy(m => m.ProductId)
                .ThenBy(m => m.Date)
                .ToList());
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(0);
    }
}

internal sealed class FakeDailyMetricsMaterializer : IDailyMetricsMaterializer
{
    public List<(IReadOnlyList<Guid> StoreIds, DateTime Date)> Calls { get; } = new();

    public int CallCount => Calls.Count;

    public Task RecomputeDayAsync(
        IReadOnlyCollection<Guid> storeIds,
        DateTime date,
        CancellationToken cancellationToken = default)
    {
        Calls.Add((storeIds.ToList(), date));

        return Task.CompletedTask;
    }
}