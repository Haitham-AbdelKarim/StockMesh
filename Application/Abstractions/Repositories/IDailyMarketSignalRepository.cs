using Domain.Entities;
using Domain.Enums;

namespace Application.Abstractions.Repositories;

public interface IDailyMarketSignalRepository
{
    Task<DailyMarketSignal?> GetByDateAsync(
        Guid productId,
        VerticalCategory verticalCategory,
        DateTime date,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DailyMarketSignal>> GetHistoryAsync(
        Guid productId,
        VerticalCategory verticalCategory,
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        DailyMarketSignal signal,
        CancellationToken cancellationToken = default);

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}