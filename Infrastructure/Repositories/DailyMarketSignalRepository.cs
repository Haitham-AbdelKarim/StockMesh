using Application.Abstractions.Repositories;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class DailyMarketSignalRepository : IDailyMarketSignalRepository
{
    private readonly AppDbContext _dbContext;

    public DailyMarketSignalRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<DailyMarketSignal?> GetByDateAsync(
        Guid productId,
        VerticalCategory verticalCategory,
        DateTime date,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.DailyMarketSignals
            .FirstOrDefaultAsync(
                s => s.ProductId == productId
                    && s.VerticalCategory == verticalCategory
                    && s.Date == date,
                cancellationToken);
    }

    public async Task AddAsync(
        DailyMarketSignal signal,
        CancellationToken cancellationToken = default)
    {
        await _dbContext.DailyMarketSignals.AddAsync(signal, cancellationToken);
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.SaveChangesAsync(cancellationToken);
    }
}