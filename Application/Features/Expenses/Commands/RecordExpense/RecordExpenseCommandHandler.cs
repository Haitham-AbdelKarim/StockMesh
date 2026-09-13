using System.Text.Json;
using Application.Abstractions.Persistence;
using Application.Abstractions.Repositories;
using Application.Abstractions.Services;
using Application.Common.Models;
using Application.DTOs.Expenses;
using Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Application.Features.Expenses.Commands.RecordExpense;

public sealed class RecordExpenseCommandHandler :
    IRequestHandler<RecordExpenseCommand, Result<ExpenseResponse>>
{
    private readonly ICurrentUser _currentUser;
    private readonly IExpenseRepository _expenseRepository;
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly IDailyMetricsMaterializer _dailyMetricsMaterializer;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<RecordExpenseCommandHandler> _logger;

    public RecordExpenseCommandHandler(
        ICurrentUser currentUser,
        IExpenseRepository expenseRepository,
        IAuditLogRepository auditLogRepository,
        IDailyMetricsMaterializer dailyMetricsMaterializer,
        IUnitOfWork unitOfWork,
        ILogger<RecordExpenseCommandHandler> logger)
    {
        _currentUser = currentUser;
        _expenseRepository = expenseRepository;
        _auditLogRepository = auditLogRepository;
        _dailyMetricsMaterializer = dailyMetricsMaterializer;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<ExpenseResponse>> Handle(
        RecordExpenseCommand command,
        CancellationToken cancellationToken)
    {
        await using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);

        var expense = new Expense(
            _currentUser.StoreId,
            command.Category,
            command.Amount,
            command.IncurredAt.ToUniversalTime(),
            command.Note);

        await _expenseRepository.AddAsync(expense, cancellationToken);

        try
        {
            await _expenseRepository.SaveChangesAsync(cancellationToken);

            await _auditLogRepository.AddAsync(new AuditLog(
                nameof(Expense),
                expense.Id,
                "expense.recorded",
                _currentUser.StoreId,
                JsonSerializer.Serialize(new
                {
                    command.Category,
                    command.Amount
                })), cancellationToken);

            await _dailyMetricsMaterializer.RecomputeDayAsync(
                [_currentUser.StoreId],
                expense.IncurredAt.Date,
                cancellationToken);

            await _expenseRepository.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogWarning(
                ex,
                "Concurrent modification while recording an expense for store {StoreId}.",
                _currentUser.StoreId);

            await transaction.RollbackAsync(cancellationToken);

            return Result<ExpenseResponse>.Conflict(
                "The analytics data changed while processing. Please try again.");
        }

        await transaction.CommitAsync(cancellationToken);

        return Result<ExpenseResponse>.Success(new ExpenseResponse(
            expense.Id,
            expense.StoreId,
            expense.Category,
            expense.Amount,
            expense.IncurredAt,
            expense.Note));
    }
}