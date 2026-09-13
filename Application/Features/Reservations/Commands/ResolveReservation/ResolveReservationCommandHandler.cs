using Application.Abstractions.Persistence;
using Application.Abstractions.Repositories;
using Application.Abstractions.Services;
using Application.Common.Models;
using Application.DTOs.Reservations;
using Domain.Entities;
using Domain.Enums;
using Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Application.Features.Reservations.Commands.ResolveReservation;

public sealed class ResolveReservationCommandHandler :
    IRequestHandler<ResolveReservationCommand, Result<ReservationResponse>>
{
    private static class AuditActions
    {
        public const string EntityType = "StockReservation";

        public const string Accepted = "reservation.accepted";

        public const string ResolvedCancelled = "reservation.resolved.cancelled";
    }

    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly IStockReservationRepository _stockReservationRepository;
    private readonly IInventoryBatchRepository _inventoryBatchRepository;
    private readonly IReservationPaymentRepository _paymentRepository;
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ResolveReservationCommandHandler> _logger;

    public ResolveReservationCommandHandler(
        ICurrentUser currentUser,
        IDateTimeProvider clock,
        IStockReservationRepository stockReservationRepository,
        IInventoryBatchRepository inventoryBatchRepository,
        IReservationPaymentRepository paymentRepository,
        IAuditLogRepository auditLogRepository,
        IUnitOfWork unitOfWork,
        ILogger<ResolveReservationCommandHandler> logger)
    {
        _currentUser = currentUser;
        _clock = clock;
        _stockReservationRepository = stockReservationRepository;
        _inventoryBatchRepository = inventoryBatchRepository;
        _paymentRepository = paymentRepository;
        _auditLogRepository = auditLogRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<ReservationResponse>> Handle(
        ResolveReservationCommand command,
        CancellationToken cancellationToken)
    {
        var reservation = await _stockReservationRepository.GetByIdAsync(command.ReservationId, cancellationToken);

        if (reservation is null)
        {
            return Result<ReservationResponse>.NotFound("Reservation not found.");
        }

        if (_currentUser.StoreId != reservation.RequestingStoreId
            && _currentUser.StoreId != reservation.OwningStoreId)
        {
            return Result<ReservationResponse>.Forbidden(
                "Only the requesting or owning store can resolve a reservation.");
        }

        if (command.Outcome == ReservationStatus.Success)
        {
            return Result<ReservationResponse>.BadRequest(
                "Success is set by payment confirmation, not by direct resolution.");
        }

        if (command.Outcome == ReservationStatus.Accepted
            && _currentUser.StoreId != reservation.OwningStoreId)
        {
            return Result<ReservationResponse>.Forbidden(
                "Only the owning store can accept a reservation.");
        }

        await using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);

        InventoryBatch? batch = null;

        if (command.Outcome == ReservationStatus.Cancelled)
        {
            batch = await _inventoryBatchRepository.GetByIdAsync(reservation.BatchId, cancellationToken);

            if (batch is null)
            {
                await transaction.RollbackAsync(cancellationToken);

                return Result<ReservationResponse>.Conflict(
                    "The reservation's inventory batch no longer exists.");
            }
        }

        try
        {
            reservation.Resolve(command.Outcome);
        }
        catch (InvalidReservationTransitionException ex)
        {
            await transaction.RollbackAsync(cancellationToken);

            return Result<ReservationResponse>.Conflict(ex.Message);
        }

        if (command.Outcome == ReservationStatus.Cancelled)
        {
            batch!.ReleaseToSharedPool(reservation.Quantity);
            _inventoryBatchRepository.Update(batch);
        }

        _stockReservationRepository.Update(reservation);

        await _auditLogRepository.AddAsync(new AuditLog(
            AuditActions.EntityType,
            reservation.Id,
            command.Outcome == ReservationStatus.Accepted
                ? AuditActions.Accepted
                : AuditActions.ResolvedCancelled,
            _currentUser.StoreId,
            metadata: null,
            _clock.UtcNow), cancellationToken);

        try
        {
            await _stockReservationRepository.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogWarning(
                ex,
                "Concurrent modification while resolving reservation {ReservationId}.",
                command.ReservationId);

            await transaction.RollbackAsync(cancellationToken);

            return Result<ReservationResponse>.Conflict(
                "The batch stock changed while processing. Please try again.");
        }

        await transaction.CommitAsync(cancellationToken);

        var payment = await _paymentRepository.GetByReservationIdAsync(
            reservation.Id,
            cancellationToken);

        var paymentState = payment is null
            ? ReservationPaymentState.Unpaid
            : ReservationMapper.ToPaymentState(payment.Status);

        return Result<ReservationResponse>.Success(ReservationMapper.ToResponse(reservation, paymentState));
    }
}