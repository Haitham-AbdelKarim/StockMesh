using Application.Abstractions.Repositories;
using Domain.Entities;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class StockMovementRepository : IStockMovementRepository
{
    private readonly AppDbContext _dbContext;

    public StockMovementRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<StockMovement?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.StockMovements
            .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<StockMovement>> GetByBatchAsync(
        Guid batchId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.StockMovements
            .Where(m => m.BatchId == batchId)
            .ToListAsync(cancellationToken);
    }

    public async Task<Guid> AddAsync(
        StockMovement movement,
        CancellationToken cancellationToken = default)
    {
        await _dbContext.StockMovements.AddAsync(movement, cancellationToken);
        return movement.Id;
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.SaveChangesAsync(cancellationToken);
    }
}