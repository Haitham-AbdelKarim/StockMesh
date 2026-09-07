using Domain.Entities;

namespace Application.Abstractions.Repositories;

public interface IDailyProductMetricRepository
{
    Task<DailyProductMetric?> GetAsync(
        Guid storeId,
        Guid productId,
        DateTime date,
        CancellationToken cancellationToken = default);

    Task<Guid> AddAsync(DailyProductMetric metric, CancellationToken cancellationToken = default);

    void Update(DailyProductMetric metric);

    Task<IReadOnlyList<DailyProductMetric>> GetRangeAsync(
        Guid storeId,
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken = default);

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}