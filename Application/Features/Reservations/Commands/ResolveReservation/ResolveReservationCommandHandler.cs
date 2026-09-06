using Application.Abstractions.Persistence;
using Application.Abstractions.Repositories;
using Application.Abstractions.Services;
using Application.Common.Models;
using Application.DTOs.Reservations;
using Application.Features.Reservations.Notifications;
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

        public const string ResolvedSuccessfully = "reservation.resolved.success";

        public const string ResolvedCancelled = "reservation.resolved.cancelled";
    }

    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly IStockReservationRepository _stockReservationRepository;
    private readonly IInventoryBatchRepository _inventoryBatchRepository;
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMediator _mediator;
    private readonly ILogger<ResolveReservationCommandHandler> _logger;

    public ResolveReservationCommandHandler(
        ICurrentUser currentUser,
        IDateTimeProvider clock,
        IStockReservationRepository stockReservationRepository,
        IInventoryBatchRepository inventoryBatchRepository,
        IAuditLogRepository auditLogRepository,
        IUnitOfWork unitOfWork,
        IMediator mediator,
        ILogger<ResolveReservationCommandHandler> logger)
    {
        _currentUser = currentUser;
        _clock = clock;
        _stockReservationRepository = stockReservationRepository;
        _inventoryBatchRepository = inventoryBatchRepository;
        _auditLogRepository = auditLogRepository;
        _unitOfWork = unitOfWork;
        _mediator = mediator;
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

        await using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);

        var batch = await _inventoryBatchRepository.GetByIdAsync(reservation.BatchId, cancellationToken);

        if (batch is null)
        {
            await transaction.RollbackAsync(cancellationToken);

            return Result<ReservationResponse>.Conflict(
                "The reservation's inventory batch no longer exists.");
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
            batch.ReleaseToSharedPool(reservation.Quantity);
            _inventoryBatchRepository.Update(batch);
        }

        _stockReservationRepository.Update(reservation);

        await _auditLogRepository.AddAsync(new AuditLog(
            AuditActions.EntityType,
            reservation.Id,
            command.Outcome == ReservationStatus.Success
                ? AuditActions.ResolvedSuccessfully
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

        if (command.Outcome == ReservationStatus.Success)
        {
            await _mediator.Publish(
                new ReservationResolvedSuccessfullyNotification(reservation.Id),
                cancellationToken);
        }

        return Result<ReservationResponse>.Success(ReservationMapper.ToResponse(reservation));
    }
}