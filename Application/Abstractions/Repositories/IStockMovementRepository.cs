using Domain.Entities;
using Domain.Enums;

namespace Application.Abstractions.Repositories;

public interface IStockMovementRepository
{
    Task<StockMovement?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<StockMovement>> GetByBatchAsync(
        Guid batchId,
        CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<StockMovement> Items, int TotalCount)> GetByStoreAsync(
        Guid storeId,
        MovementType? movementType,
        DateTime? from,
        DateTime? to,
        Guid? relatedStoreId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<StockMovement>> GetForStoresAsync(
        IReadOnlyCollection<Guid> storeIds,
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<StockMovement>> GetByTypeInRangeAsync(
        MovementType movementType,
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken = default);

    Task<Guid> AddAsync(StockMovement movement, CancellationToken cancellationToken = default);

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}