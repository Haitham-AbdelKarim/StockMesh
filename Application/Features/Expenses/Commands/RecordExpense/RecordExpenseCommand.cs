using Application.Common.Models;
using Application.DTOs.Expenses;
using MediatR;

namespace Application.Features.Expenses.Commands.RecordExpense;

public sealed record RecordExpenseCommand(
    string Category,
    decimal Amount,
    DateTime IncurredAt,
    string? Note = null) : IRequest<Result<ExpenseResponse>>;