using System.Text.Json;
using Application.Abstractions.Persistence;
using Application.Abstractions.Repositories;
using Application.Abstractions.Services;
using Application.Common.Models;
using Application.DTOs.StockMovements;
using Domain.Entities;
using Domain.Enums;
using Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Application.Features.StockMovements.Commands.RecordSale;

public sealed class RecordSaleCommandHandler :
    IRequestHandler<RecordSaleCommand, Result<SaleResponse>>
{
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly IInventoryBatchRepository _inventoryBatchRepository;
    private readonly IStockMovementRepository _stockMovementRepository;
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly IDailyMetricsMaterializer _dailyMetricsMaterializer;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<RecordSaleCommandHandler> _logger;

    public RecordSaleCommandHandler(
        ICurrentUser currentUser,
        IDateTimeProvider clock,
        IInventoryBatchRepository inventoryBatchRepository,
        IStockMovementRepository stockMovementRepository,
        IAuditLogRepository auditLogRepository,
        IDailyMetricsMaterializer dailyMetricsMaterializer,
        IUnitOfWork unitOfWork,
        ILogger<RecordSaleCommandHandler> logger)
    {
        _currentUser = currentUser;
        _clock = clock;
        _inventoryBatchRepository = inventoryBatchRepository;
        _stockMovementRepository = stockMovementRepository;
        _auditLogRepository = auditLogRepository;
        _dailyMetricsMaterializer = dailyMetricsMaterializer;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<SaleResponse>> Handle(
        RecordSaleCommand command,
        CancellationToken cancellationToken)
    {
        await using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);

        var batch = await _inventoryBatchRepository.GetByIdAsync(command.BatchId, cancellationToken);

        if (batch is null)
        {
            await transaction.RollbackAsync(cancellationToken);

            return Result<SaleResponse>.NotFound("Inventory batch not found.");
        }

        if (batch.StoreId != _currentUser.StoreId)
        {
            await transaction.RollbackAsync(cancellationToken);

            return Result<SaleResponse>.Forbidden("Cannot sell from another store's batch.");
        }

        try
        {
            batch.ConsumePrivate(command.Quantity);
        }
        catch (InsufficientStockException ex)
        {
            await transaction.RollbackAsync(cancellationToken);

            return Result<SaleResponse>.Conflict(ex.Message);
        }

        var movement = new StockMovement(
            _currentUser.StoreId,
            command.BatchId,
            MovementType.Sale,
            command.Quantity,
            _clock.UtcNow,
            unitPrice: batch.UnitSalePrice,
            unitCost: batch.UnitCost,
            staffUserId: _currentUser.UserId);

        _inventoryBatchRepository.Update(batch);
        await _stockMovementRepository.AddAsync(movement, cancellationToken);

        try
        {
            await _stockMovementRepository.SaveChangesAsync(cancellationToken);

            await _auditLogRepository.AddAsync(new AuditLog(
                nameof(StockMovement),
                movement.Id,
                "sale.recorded",
                _currentUser.StoreId,
                JsonSerializer.Serialize(new
                {
                    command.Quantity,
                    unitPrice = batch.UnitSalePrice,
                    total = command.Quantity * batch.UnitSalePrice
                })), cancellationToken);

            await _dailyMetricsMaterializer.RecomputeDayAsync(
                [_currentUser.StoreId],
                _clock.UtcNow.Date,
                cancellationToken);

            await _stockMovementRepository.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogWarning(
                ex,
                "Concurrent modification while recording a sale for batch {BatchId}.",
                command.BatchId);

            await transaction.RollbackAsync(cancellationToken);

            return Result<SaleResponse>.Conflict(
                "The batch stock changed while processing. Please try again.");
        }

        await transaction.CommitAsync(cancellationToken);

        return Result<SaleResponse>.Success(new SaleResponse(
            movement.Id,
            command.BatchId,
            command.Quantity,
            batch.UnitSalePrice,
            command.Quantity * batch.UnitSalePrice));
    }
}