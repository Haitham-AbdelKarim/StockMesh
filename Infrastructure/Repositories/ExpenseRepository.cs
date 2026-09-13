using Application.Abstractions.Repositories;
using Domain.Entities;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class ExpenseRepository : IExpenseRepository
{
    private readonly AppDbContext _dbContext;

    public ExpenseRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Guid> AddAsync(
        Expense expense,
        CancellationToken cancellationToken = default)
    {
        await _dbContext.Expenses.AddAsync(expense, cancellationToken);

        return expense.Id;
    }

    public async Task<(IReadOnlyList<Expense> Items, int TotalCount)> GetByStoreAsync(
        Guid storeId,
        string? category,
        DateTime? from,
        DateTime? to,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Expenses
            .Where(e => e.StoreId == storeId);

        if (!string.IsNullOrWhiteSpace(category))
        {
            query = query.Where(e => e.Category == category);
        }

        if (from is { } fromDate)
        {
            query = query.Where(e => e.IncurredAt >= fromDate);
        }

        if (to is { } toDate)
        {
            query = query.Where(e => e.IncurredAt <= toDate);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(e => e.IncurredAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<IReadOnlyList<Expense>> GetForStoresAsync(
        IReadOnlyCollection<Guid> storeIds,
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Expenses
            .Where(e => storeIds.Contains(e.StoreId) && e.IncurredAt >= from && e.IncurredAt < to)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.SaveChangesAsync(cancellationToken);
    }
}