using Application.Abstractions.Repositories;
using Application.Abstractions.Services;
using Application.Common.Models;
using Application.DTOs.Expenses;
using MediatR;

namespace Application.Features.Expenses.Queries.GetExpenses;

public sealed class GetExpensesQueryHandler :
    IRequestHandler<GetExpensesQuery, Result<PaginatedList<ExpenseResponse>>>
{
    private readonly ICurrentUser _currentUser;
    private readonly IExpenseRepository _expenseRepository;

    public GetExpensesQueryHandler(
        ICurrentUser currentUser,
        IExpenseRepository expenseRepository)
    {
        _currentUser = currentUser;
        _expenseRepository = expenseRepository;
    }

    public async Task<Result<PaginatedList<ExpenseResponse>>> Handle(
        GetExpensesQuery query,
        CancellationToken cancellationToken)
    {
        var (items, totalCount) = await _expenseRepository.GetByStoreAsync(
            _currentUser.StoreId,
            query.Category,
            query.From,
            query.To,
            query.Page,
            query.PageSize,
            cancellationToken);

        var responseItems = items
            .Select(e => new ExpenseResponse(
                e.Id,
                e.StoreId,
                e.Category,
                e.Amount,
                e.IncurredAt,
                e.Note))
            .ToList();

        var paginated = PaginatedList<ExpenseResponse>.Create(
            responseItems,
            totalCount,
            query.Page,
            query.PageSize);

        return Result<PaginatedList<ExpenseResponse>>.Success(paginated);
    }
}