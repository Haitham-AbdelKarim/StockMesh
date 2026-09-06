using Application.Abstractions.Models;
using Application.Abstractions.Repositories;
using Domain.Entities;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class InventoryBatchRepository : IInventoryBatchRepository
{
    private readonly AppDbContext _dbContext;

    public InventoryBatchRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<InventoryBatch?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.InventoryBatches
            .FirstOrDefaultAsync(b => b.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<InventoryBatch>> GetByProductAsync(
        Guid productId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.InventoryBatches
            .Where(b => b.ProductId == productId)
            .ToListAsync(cancellationToken);
    }

    public async Task<InventoryBatch?> GetLatestByProductAsync(
        Guid storeId,
        Guid productId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.InventoryBatches
            .Where(b => b.StoreId == storeId && b.ProductId == productId)
            .OrderByDescending(b => b.ReceivedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<(IReadOnlyList<InventoryBatch> Items, int TotalCount)> GetBatchesAsync(
        Guid storeId,
        Guid? productId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.InventoryBatches
            .Where(b => b.StoreId == storeId);

        if (productId is { } id)
        {
            query = query.Where(b => b.ProductId == id);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(b => b.ReceivedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<IReadOnlyList<ProductStockSummary>> GetProductStockSummariesAsync(
        Guid storeId,
        CancellationToken cancellationToken = default)
    {
        var batches = await _dbContext.InventoryBatches
            .Where(b => b.StoreId == storeId)
            .ToListAsync(cancellationToken);

        return batches
            .GroupBy(b => b.ProductId)
            .Select(g => new ProductStockSummary(
                g.Key,
                g.Sum(b => b.QuantityRemaining),
                g.Sum(b => b.SharedQuantity),
                g.Count()))
            .OrderBy(s => s.ProductId)
            .ToList();
    }

    public async Task<IReadOnlyList<InventoryBatch>> GetSharedByStoresAsync(
        IReadOnlyCollection<Guid> storeIds,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.InventoryBatches
            .Where(b => storeIds.Contains(b.StoreId) && b.SharedQuantity > 0)
            .ToListAsync(cancellationToken);
    }

    public async Task<Guid> AddAsync(
        InventoryBatch batch,
        CancellationToken cancellationToken = default)
    {
        await _dbContext.InventoryBatches.AddAsync(batch, cancellationToken);
        return batch.Id;
    }

    public void Update(InventoryBatch batch)
    {
        _dbContext.InventoryBatches.Update(batch);
    }

    public void Remove(InventoryBatch batch)
    {
        _dbContext.InventoryBatches.Remove(batch);
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.SaveChangesAsync(cancellationToken);
    }
}