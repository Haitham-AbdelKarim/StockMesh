using Domain.Entities;
using Domain.Enums;

namespace Application.Abstractions.Repositories;

public interface IRecommendationRepository
{
    Task<IReadOnlyList<Recommendation>> GetForProductAsync(
        Guid storeId,
        Guid productId,
        CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<Recommendation> Items, int TotalCount)> GetForStoreAsync(
        Guid storeId,
        RecommendedAction? recommendedAction,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        Recommendation recommendation,
        CancellationToken cancellationToken = default);

    void RemoveRange(
        IEnumerable<Recommendation> recommendations);

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}