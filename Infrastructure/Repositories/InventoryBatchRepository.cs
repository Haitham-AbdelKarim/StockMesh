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