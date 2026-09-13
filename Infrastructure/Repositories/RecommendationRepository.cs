using Application.Abstractions.Repositories;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class RecommendationRepository : IRecommendationRepository
{
    private readonly AppDbContext _dbContext;

    public RecommendationRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<Recommendation>> GetForProductAsync(
        Guid storeId,
        Guid productId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Recommendations
            .Where(r => r.StoreId == storeId && r.ProductId == productId)
            .ToListAsync(cancellationToken);
    }

    public async Task<(IReadOnlyList<Recommendation> Items, int TotalCount)> GetForStoreAsync(
        Guid storeId,
        RecommendedAction? recommendedAction,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Recommendations
            .Where(r => r.StoreId == storeId);

        if (recommendedAction is { } action)
        {
            query = query.Where(r => r.RecommendedAction == action);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(r => r.GeneratedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task AddAsync(
        Recommendation recommendation,
        CancellationToken cancellationToken = default)
    {
        await _dbContext.Recommendations.AddAsync(recommendation, cancellationToken);
    }

    public void RemoveRange(
        IEnumerable<Recommendation> recommendations)
    {
        _dbContext.Recommendations.RemoveRange(recommendations);
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.SaveChangesAsync(cancellationToken);
    }
}