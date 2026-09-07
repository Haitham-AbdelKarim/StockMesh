using Application.Abstractions.Repositories;
using Domain.Entities;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class DailyStoreMetricRepository : IDailyStoreMetricRepository
{
    private readonly AppDbContext _dbContext;

    public DailyStoreMetricRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<DailyStoreMetric?> GetAsync(
        Guid storeId,
        DateTime date,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.DailyStoreMetrics
            .FirstOrDefaultAsync(m => m.StoreId == storeId && m.Date == date, cancellationToken);
    }

    public async Task<Guid> AddAsync(
        DailyStoreMetric metric,
        CancellationToken cancellationToken = default)
    {
        await _dbContext.DailyStoreMetrics.AddAsync(metric, cancellationToken);

        return metric.Id;
    }

    public void Update(DailyStoreMetric metric)
    {
        _dbContext.DailyStoreMetrics.Update(metric);
    }

    public async Task<IReadOnlyList<DailyStoreMetric>> GetRangeAsync(
        Guid storeId,
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.DailyStoreMetrics
            .Where(m => m.StoreId == storeId && m.Date >= from && m.Date < to)
            .OrderBy(m => m.Date)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.SaveChangesAsync(cancellationToken);
    }
}