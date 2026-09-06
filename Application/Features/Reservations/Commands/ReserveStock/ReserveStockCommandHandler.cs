using System.Text.Json;
using Application.Abstractions.Locking;
using Application.Abstractions.Options;
using Application.Abstractions.Persistence;
using Application.Abstractions.Repositories;
using Application.Abstractions.Services;
using Application.Common.Models;
using Application.DTOs.Reservations;
using Domain.Entities;
using Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Application.Features.Reservations.Commands.ReserveStock;

public sealed class ReserveStockCommandHandler :
    IRequestHandler<ReserveStockCommand, Result<ReservationResponse>>
{
    private static class AuditActions
    {
        public const string EntityType = "StockReservation";

        public const string Created = "reservation.created";
    }

    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly IInventoryBatchRepository _inventoryBatchRepository;
    private readonly IStockReservationRepository _stockReservationRepository;
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly IReservationLockService _lockService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ReservationOptions _options;
    private readonly ILogger<ReserveStockCommandHandler> _logger;

    public ReserveStockCommandHandler(
        ICurrentUser currentUser,
        IDateTimeProvider clock,
        IInventoryBatchRepository inventoryBatchRepository,
        IStockReservationRepository stockReservationRepository,
        IAuditLogRepository auditLogRepository,
        IReservationLockService lockService,
        IUnitOfWork unitOfWork,
        ReservationOptions options,
        ILogger<ReserveStockCommandHandler> logger)
    {
        _currentUser = currentUser;
        _clock = clock;
        _inventoryBatchRepository = inventoryBatchRepository;
        _stockReservationRepository = stockReservationRepository;
        _auditLogRepository = auditLogRepository;
        _lockService = lockService;
        _unitOfWork = unitOfWork;
        _options = options;
        _logger = logger;
    }

    public async Task<Result<ReservationResponse>> Handle(
        ReserveStockCommand command,
        CancellationToken cancellationToken)
    {
        var token = Guid.NewGuid().ToString("N");

        if (!await _lockService.AcquireAsync(command.BatchId, token, cancellationToken))
        {
            return Result<ReservationResponse>.Conflict(
                "The batch is currently being processed by another requester. Please try again.");
        }

        try
        {
            await using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);

            var batch = await _inventoryBatchRepository.GetByIdAsync(command.BatchId, cancellationToken);

            if (batch is null)
            {
                await transaction.RollbackAsync(cancellationToken);

                return Result<ReservationResponse>.NotFound("Inventory batch not found.");
            }

            if (batch.StoreId == _currentUser.StoreId)
            {
                await transaction.RollbackAsync(cancellationToken);

                return Result<ReservationResponse>.BadRequest(
                    "A store cannot reserve its own shared stock.");
            }

            try
            {
                batch.ReserveFromSharedPool(command.Quantity);
            }
            catch (InsufficientStockException ex)
            {
                await transaction.RollbackAsync(cancellationToken);

                return Result<ReservationResponse>.Conflict(ex.Message);
            }

            var reservation = new StockReservation(
                command.BatchId,
                _currentUser.StoreId,
                batch.StoreId,
                command.Quantity,
                batch.UnitSalePrice,
                command.DistanceKm,
                command.DeliveryEta,
                holdMinutes: _options.HoldMinutes,
                holdExpiresAt: _clock.UtcNow.AddMinutes(_options.HoldMinutes));

            _inventoryBatchRepository.Update(batch);
            await _stockReservationRepository.AddAsync(reservation, cancellationToken);
            await _auditLogRepository.AddAsync(new AuditLog(
                AuditActions.EntityType,
                reservation.Id,
                AuditActions.Created,
                _currentUser.StoreId,
                JsonSerializer.Serialize(new
                {
                    batchId = command.BatchId,
                    quantity = command.Quantity,
                    unitPrice = batch.UnitSalePrice
                }),
                _clock.UtcNow), cancellationToken);

            try
            {
                await _stockReservationRepository.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogWarning(
                    ex,
                    "Concurrent modification while reserving stock for batch {BatchId}.",
                    command.BatchId);

                await transaction.RollbackAsync(cancellationToken);

                return Result<ReservationResponse>.Conflict(
                    "The batch stock changed while processing. Please try again.");
            }

            await transaction.CommitAsync(cancellationToken);

            return Result<ReservationResponse>.Success(ReservationMapper.ToResponse(reservation));
        }
        finally
        {
            await _lockService.ReleaseAsync(command.BatchId, token, cancellationToken);
        }
    }
}