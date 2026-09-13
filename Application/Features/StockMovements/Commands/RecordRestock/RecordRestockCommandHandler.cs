using System.Text.Json;
using Application.Abstractions.Persistence;
using Application.Abstractions.Repositories;
using Application.Abstractions.Services;
using Application.Common.Models;
using Application.DTOs.StockMovements;
using Domain.Entities;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Application.Features.StockMovements.Commands.RecordRestock;

public sealed class RecordRestockCommandHandler :
    IRequestHandler<RecordRestockCommand, Result<RestockResponse>>
{
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly IInventoryBatchRepository _inventoryBatchRepository;
    private readonly IProductRepository _productRepository;
    private readonly IStockMovementRepository _stockMovementRepository;
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly IDailyMetricsMaterializer _dailyMetricsMaterializer;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<RecordRestockCommandHandler> _logger;

    public RecordRestockCommandHandler(
        ICurrentUser currentUser,
        IDateTimeProvider clock,
        IInventoryBatchRepository inventoryBatchRepository,
        IProductRepository productRepository,
        IStockMovementRepository stockMovementRepository,
        IAuditLogRepository auditLogRepository,
        IDailyMetricsMaterializer dailyMetricsMaterializer,
        IUnitOfWork unitOfWork,
        ILogger<RecordRestockCommandHandler> logger)
    {
        _currentUser = currentUser;
        _clock = clock;
        _inventoryBatchRepository = inventoryBatchRepository;
        _productRepository = productRepository;
        _stockMovementRepository = stockMovementRepository;
        _auditLogRepository = auditLogRepository;
        _dailyMetricsMaterializer = dailyMetricsMaterializer;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<RestockResponse>> Handle(
        RecordRestockCommand command,
        CancellationToken cancellationToken)
    {
        var product = await _productRepository.GetByIdAsync(command.ProductId, cancellationToken);

        if (product is null)
        {
            return Result<RestockResponse>.BadRequest("Product not found.");
        }

        if (product.VerticalCategory != _currentUser.VerticalCategory)
        {
            return Result<RestockResponse>.BadRequest(
                "Product does not belong to your vertical.");
        }

        await using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);

        var previousBatch = await _inventoryBatchRepository.GetLatestByProductAsync(
            _currentUser.StoreId,
            command.ProductId,
            cancellationToken);

        var unitSalePrice = previousBatch is null ? command.UnitCost : previousBatch.UnitSalePrice;

        var batch = new InventoryBatch(
            _currentUser.StoreId,
            command.ProductId,
            command.Quantity,
            command.UnitCost,
            unitSalePrice,
            expiryDate: command.ExpiryDate);

        await _inventoryBatchRepository.AddAsync(batch, cancellationToken);

        var movement = new StockMovement(
            _currentUser.StoreId,
            batch.Id,
            MovementType.Restock,
            command.Quantity,
            _clock.UtcNow,
            unitCost: command.UnitCost,
            supplierName: command.SupplierName);

        await _stockMovementRepository.AddAsync(movement, cancellationToken);

        try
        {
            await _stockMovementRepository.SaveChangesAsync(cancellationToken);

            await _auditLogRepository.AddAsync(new AuditLog(
                nameof(InventoryBatch),
                batch.Id,
                "restock.recorded",
                _currentUser.StoreId,
                JsonSerializer.Serialize(new
                {
                    command.Quantity,
                    command.UnitCost,
                    command.SupplierName
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
                "Concurrent modification while recording a restock for product {ProductId}.",
                command.ProductId);

            await transaction.RollbackAsync(cancellationToken);

            return Result<RestockResponse>.Conflict(
                "The stock changed while processing. Please try again.");
        }

        await transaction.CommitAsync(cancellationToken);

        return Result<RestockResponse>.Success(new RestockResponse(
            batch.Id,
            command.ProductId,
            command.Quantity,
            command.UnitCost,
            unitSalePrice));
    }
}