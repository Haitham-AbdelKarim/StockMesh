using Domain.Entities;

namespace Application.Abstractions.Repositories;

public interface IExpenseRepository
{
    Task<Guid> AddAsync(Expense expense, CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<Expense> Items, int TotalCount)> GetByStoreAsync(
        Guid storeId,
        string? category,
        DateTime? from,
        DateTime? to,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Expense>> GetForStoresAsync(
        IReadOnlyCollection<Guid> storeIds,
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken = default);

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}