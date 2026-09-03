using Domain.Entities;

namespace Application.Abstractions.Repositories;

public interface IStockMovementRepository
{
    Task<StockMovement?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<StockMovement>> GetByBatchAsync(
        Guid batchId,
        CancellationToken cancellationToken = default);

    Task<Guid> AddAsync(StockMovement movement, CancellationToken cancellationToken = default);
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

}