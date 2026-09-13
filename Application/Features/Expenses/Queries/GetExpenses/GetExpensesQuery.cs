using Application.Common.Models;
using Application.DTOs.Expenses;
using MediatR;

namespace Application.Features.Expenses.Queries.GetExpenses;

public sealed record GetExpensesQuery(
    string? Category = null,
    DateTime? From = null,
    DateTime? To = null,
    int Page = 1,
    int PageSize = 20) : IRequest<Result<PaginatedList<ExpenseResponse>>>;