using Domain.Entities;

namespace Application.Abstractions.Repositories;

public interface IInventoryBatchRepository
{
    Task<InventoryBatch?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<InventoryBatch>> GetByProductAsync(Guid productId, CancellationToken cancellationToken = default);

    Task<Guid> AddAsync(InventoryBatch batch, CancellationToken cancellationToken = default);

    void Update(InventoryBatch batch);

    void Remove(InventoryBatch batch);
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}