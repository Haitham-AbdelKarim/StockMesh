using Application.Abstractions.Repositories;
using Domain.Entities;
using Domain.Enums;
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

    public async Task<(IReadOnlyList<StockMovement> Items, int TotalCount)> GetByStoreAsync(
        Guid storeId,
        MovementType? movementType,
        DateTime? from,
        DateTime? to,
        Guid? relatedStoreId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.StockMovements
            .Where(m => m.StoreId == storeId);

        if (movementType is { } type)
        {
            query = query.Where(m => m.MovementType == type);
        }

        if (from is { } fromDate)
        {
            query = query.Where(m => m.OccurredAt >= fromDate);
        }

        if (to is { } toDate)
        {
            query = query.Where(m => m.OccurredAt <= toDate);
        }

        if (relatedStoreId is { } related)
        {
            query = query.Where(m => m.RelatedStoreId == related);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(m => m.OccurredAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<IReadOnlyList<StockMovement>> GetForStoresAsync(
        IReadOnlyCollection<Guid> storeIds,
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.StockMovements
            .Where(m => storeIds.Contains(m.StoreId) && m.OccurredAt >= from && m.OccurredAt < to)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<StockMovement>> GetByTypeInRangeAsync(
        MovementType movementType,
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.StockMovements
            .Where(m => m.MovementType == movementType && m.OccurredAt >= from && m.OccurredAt < to)
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