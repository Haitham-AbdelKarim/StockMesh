using System.Text.Json;
using Application.Abstractions.Persistence;
using Application.Abstractions.Repositories;
using Application.Abstractions.Services;
using Domain.Entities;
using Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Features.Reservations.Notifications;

public sealed class ReservationResolvedSuccessfullyNotificationHandler :
    INotificationHandler<ReservationResolvedSuccessfullyNotification>
{
    private readonly IStockReservationRepository _stockReservationRepository;
    private readonly IInventoryBatchRepository _inventoryBatchRepository;
    private readonly IStockMovementRepository _stockMovementRepository;
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly IDailyMetricsMaterializer _dailyMetricsMaterializer;
    private readonly IDateTimeProvider _clock;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ReservationResolvedSuccessfullyNotificationHandler> _logger;

    public ReservationResolvedSuccessfullyNotificationHandler(
        IStockReservationRepository stockReservationRepository,
        IInventoryBatchRepository inventoryBatchRepository,
        IStockMovementRepository stockMovementRepository,
        IAuditLogRepository auditLogRepository,
        IDailyMetricsMaterializer dailyMetricsMaterializer,
        IDateTimeProvider clock,
        IUnitOfWork unitOfWork,
        ILogger<ReservationResolvedSuccessfullyNotificationHandler> logger)
    {
        _stockReservationRepository = stockReservationRepository;
        _inventoryBatchRepository = inventoryBatchRepository;
        _stockMovementRepository = stockMovementRepository;
        _auditLogRepository = auditLogRepository;
        _dailyMetricsMaterializer = dailyMetricsMaterializer;
        _clock = clock;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task Handle(
        ReservationResolvedSuccessfullyNotification notification,
        CancellationToken cancellationToken)
    {
        try
        {
            var reservation = await _stockReservationRepository.GetByIdAsync(
                notification.ReservationId,
                cancellationToken);

            if (reservation is null || reservation.Status != ReservationStatus.Success)
            {
                return;
            }

            var ownerBatch = await _inventoryBatchRepository.GetByIdAsync(
                reservation.BatchId,
                cancellationToken);

            if (ownerBatch is null)
            {
                _logger.LogWarning(
                    "Could not record network transfer for reservation {ReservationId}: owner batch {BatchId} not found.",
                    notification.ReservationId,
                    reservation.BatchId);

                return;
            }

            await using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);

            var occurredAt = _clock.UtcNow;

            await _stockMovementRepository.AddAsync(new StockMovement(
                reservation.OwningStoreId,
                reservation.BatchId,
                MovementType.NetworkTransferOut,
                reservation.Quantity,
                occurredAt,
                reservation.RequestingStoreId,
                unitPrice: reservation.UnitPrice,
                unitCost: ownerBatch.UnitCost), cancellationToken);

            var previousBatch = await _inventoryBatchRepository.GetLatestByProductAsync(
                reservation.RequestingStoreId,
                ownerBatch.ProductId,
                cancellationToken);

            var unitSalePrice = previousBatch is null
                ? reservation.UnitPrice
                : previousBatch.UnitSalePrice;

            var incomingBatch = new InventoryBatch(
                reservation.RequestingStoreId,
                ownerBatch.ProductId,
                reservation.Quantity,
                reservation.UnitPrice,
                unitSalePrice);

            await _inventoryBatchRepository.AddAsync(incomingBatch, cancellationToken);

            await _stockMovementRepository.AddAsync(new StockMovement(
                reservation.RequestingStoreId,
                incomingBatch.Id,
                MovementType.NetworkTransferIn,
                reservation.Quantity,
                occurredAt,
                reservation.OwningStoreId,
                unitPrice: reservation.UnitPrice), cancellationToken);

            await _auditLogRepository.AddAsync(new AuditLog(
                nameof(InventoryBatch),
                ownerBatch.Id,
                "transfer.network_out",
                reservation.OwningStoreId,
                JsonSerializer.Serialize(new
                {
                    reservationId = reservation.Id,
                    requestingStoreId = reservation.RequestingStoreId,
                    reservation.Quantity
                })), cancellationToken);

            await _auditLogRepository.AddAsync(new AuditLog(
                nameof(InventoryBatch),
                incomingBatch.Id,
                "transfer.network_in",
                reservation.RequestingStoreId,
                JsonSerializer.Serialize(new
                {
                    reservationId = reservation.Id,
                    owningStoreId = reservation.OwningStoreId,
                    reservation.Quantity
                })), cancellationToken);

            await _stockMovementRepository.SaveChangesAsync(cancellationToken);

            await _dailyMetricsMaterializer.RecomputeDayAsync(
                [reservation.OwningStoreId, reservation.RequestingStoreId],
                occurredAt.Date,
                cancellationToken);

            await _stockMovementRepository.SaveChangesAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to record network transfer for reservation {ReservationId}.",
                notification.ReservationId);
        }
    }
}