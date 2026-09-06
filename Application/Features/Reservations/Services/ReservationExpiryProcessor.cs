using Application.Abstractions.Persistence;
using Application.Abstractions.Repositories;
using Application.Abstractions.Services;
using Domain.Entities;
using Domain.Enums;
using Domain.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Application.Features.Reservations.Services;

public sealed class ReservationExpiryProcessor
{
    private static class AuditActions
    {
        public const string EntityType = "StockReservation";

        public const string Expired = "reservation.expired";
    }

    private readonly IDateTimeProvider _clock;
    private readonly IStockReservationRepository _stockReservationRepository;
    private readonly IInventoryBatchRepository _inventoryBatchRepository;
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ReservationExpiryProcessor> _logger;

    public ReservationExpiryProcessor(
        IDateTimeProvider clock,
        IStockReservationRepository stockReservationRepository,
        IInventoryBatchRepository inventoryBatchRepository,
        IAuditLogRepository auditLogRepository,
        IUnitOfWork unitOfWork,
        ILogger<ReservationExpiryProcessor> logger)
    {
        _clock = clock;
        _stockReservationRepository = stockReservationRepository;
        _inventoryBatchRepository = inventoryBatchRepository;
        _auditLogRepository = auditLogRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        var now = _clock.UtcNow;

        var expired = await _stockReservationRepository.GetExpiredPendingAsync(now, cancellationToken);

        foreach (var reservation in expired)
        {
            try
            {
                await ReclaimAsync(reservation, now, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to reclaim expired reservation {ReservationId}.",
                    reservation.Id);
            }
        }
    }

    private async Task ReclaimAsync(
        StockReservation reservation,
        DateTime now,
        CancellationToken cancellationToken)
    {
        await using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);

        var batch = await _inventoryBatchRepository.GetByIdAsync(reservation.BatchId, cancellationToken);

        if (batch is null)
        {
            await transaction.RollbackAsync(cancellationToken);

            _logger.LogWarning(
                "Expired reservation {ReservationId} references a batch that no longer exists.",
                reservation.Id);

            return;
        }

        try
        {
            reservation.Resolve(ReservationStatus.Cancelled);
        }
        catch (InvalidReservationTransitionException ex)
        {
            await transaction.RollbackAsync(cancellationToken);

            _logger.LogWarning(
                ex,
                "Expired reservation {ReservationId} was already resolved.",
                reservation.Id);

            return;
        }

        batch.ReleaseToSharedPool(reservation.Quantity);
        _inventoryBatchRepository.Update(batch);
        _stockReservationRepository.Update(reservation);

        await _auditLogRepository.AddAsync(new AuditLog(
            AuditActions.EntityType,
            reservation.Id,
            AuditActions.Expired,
            actorStoreId: null,
            metadata: null,
            createdAt: now), cancellationToken);

        try
        {
            await _stockReservationRepository.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogWarning(
                ex,
                "Concurrent modification while reclaiming expired reservation {ReservationId}.",
                reservation.Id);

            await transaction.RollbackAsync(cancellationToken);

            return;
        }

        await transaction.CommitAsync(cancellationToken);
    }
}