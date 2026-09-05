using Domain.Entities;
using Domain.Enums;

namespace Application.Abstractions.Repositories;

public interface IProductRepository
{
    Task<(IReadOnlyList<Product> Items, int TotalCount)> GetAsync(
        VerticalCategory? verticalCategory,
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
}