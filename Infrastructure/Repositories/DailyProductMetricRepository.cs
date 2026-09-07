using Application.Abstractions.Repositories;
using Domain.Entities;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class DailyProductMetricRepository : IDailyProductMetricRepository
{
    private readonly AppDbContext _dbContext;

    public DailyProductMetricRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<DailyProductMetric?> GetAsync(
        Guid storeId,
        Guid productId,
        DateTime date,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.DailyProductMetrics
            .FirstOrDefaultAsync(
                m => m.StoreId == storeId && m.ProductId == productId && m.Date == date,
                cancellationToken);
    }

    public async Task<Guid> AddAsync(
        DailyProductMetric metric,
        CancellationToken cancellationToken = default)
    {
        await _dbContext.DailyProductMetrics.AddAsync(metric, cancellationToken);

        return metric.Id;
    }

    public void Update(DailyProductMetric metric)
    {
        _dbContext.DailyProductMetrics.Update(metric);
    }

    public async Task<IReadOnlyList<DailyProductMetric>> GetRangeAsync(
        Guid storeId,
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.DailyProductMetrics
            .Where(m => m.StoreId == storeId && m.Date >= from && m.Date < to)
            .OrderBy(m => m.ProductId)
            .ThenBy(m => m.Date)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.SaveChangesAsync(cancellationToken);
    }
}