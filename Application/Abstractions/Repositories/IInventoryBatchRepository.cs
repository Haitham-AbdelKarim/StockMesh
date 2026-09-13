using Application.Abstractions.Models;
using Domain.Entities;

namespace Application.Abstractions.Repositories;

public interface IInventoryBatchRepository
{
    Task<InventoryBatch?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<InventoryBatch>> GetByIdsAsync(
        IReadOnlyCollection<Guid> ids,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<InventoryBatch>> GetByProductAsync(Guid productId, CancellationToken cancellationToken = default);

    Task<InventoryBatch?> GetLatestByProductAsync(
        Guid storeId,
        Guid productId,
        CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<InventoryBatch> Items, int TotalCount)> GetBatchesAsync(
        Guid storeId,
        Guid? productId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProductStockSummary>> GetProductStockSummariesAsync(
        Guid storeId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<LowStockItem>> GetLowStockItemsAsync(
        Guid storeId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<InventoryBatch>> GetSharedByStoresAsync(
        IReadOnlyCollection<Guid> storeIds,
        CancellationToken cancellationToken = default);

    Task<Guid> AddAsync(InventoryBatch batch, CancellationToken cancellationToken = default);

    void Update(InventoryBatch batch);

    void Remove(InventoryBatch batch);

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}