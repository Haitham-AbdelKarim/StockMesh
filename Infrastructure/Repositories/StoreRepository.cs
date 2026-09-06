using Application.Abstractions.Repositories;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class StoreRepository : IStoreRepository
{
    private readonly AppDbContext _dbContext;

    public StoreRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Store?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Stores
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Store>> GetByVerticalAsync(
        VerticalCategory verticalCategory,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Stores
            .Where(s => s.VerticalCategory == verticalCategory)
            .ToListAsync(cancellationToken);
    }

    public async Task<Guid> AddAsync(
        Store store,
        CancellationToken cancellationToken = default)
    {
        await _dbContext.Stores.AddAsync(store, cancellationToken);
        return store.Id;
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.SaveChangesAsync(cancellationToken);
    }
}