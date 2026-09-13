using Domain.Entities;

namespace Application.Abstractions.Repositories;

public interface IDailyStoreMetricRepository
{
    Task<DailyStoreMetric?> GetAsync(
        Guid storeId,
        DateTime date,
        CancellationToken cancellationToken = default);

    Task<Guid> AddAsync(DailyStoreMetric metric, CancellationToken cancellationToken = default);

    void Update(DailyStoreMetric metric);

    Task<IReadOnlyList<DailyStoreMetric>> GetRangeAsync(
        Guid storeId,
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken = default);

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}