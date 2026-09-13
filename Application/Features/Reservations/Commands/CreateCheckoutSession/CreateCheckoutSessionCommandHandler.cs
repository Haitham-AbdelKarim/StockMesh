using Application.Abstractions.Persistence;
using Application.Abstractions.Repositories;
using Application.Abstractions.Services;
using Application.Common.Models;
using Application.DTOs.Payments;
using Application.Exceptions;
using Domain.Entities;
using Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Features.Reservations.Commands.CreateCheckoutSession;

public sealed class CreateCheckoutSessionCommandHandler :
    IRequestHandler<CreateCheckoutSessionCommand, Result<CheckoutSessionResponse>>
{
    private static class AuditActions
    {
        public const string EntityType = "ReservationPayment";

        public const string CheckoutCreated = "payment.checkout.created";

        public const string OwnerNotOnboarded = "payment.owner-not-onboarded";
    }

    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly IStockReservationRepository _stockReservationRepository;
    private readonly IStoreRepository _storeRepository;
    private readonly IReservationPaymentRepository _paymentRepository;
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly IPaymentService _paymentService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CreateCheckoutSessionCommandHandler> _logger;

    public CreateCheckoutSessionCommandHandler(
        ICurrentUser currentUser,
        IDateTimeProvider clock,
        IStockReservationRepository stockReservationRepository,
        IStoreRepository storeRepository,
        IReservationPaymentRepository paymentRepository,
        IAuditLogRepository auditLogRepository,
        IPaymentService paymentService,
        IUnitOfWork unitOfWork,
        ILogger<CreateCheckoutSessionCommandHandler> logger)
    {
        _currentUser = currentUser;
        _clock = clock;
        _stockReservationRepository = stockReservationRepository;
        _storeRepository = storeRepository;
        _paymentRepository = paymentRepository;
        _auditLogRepository = auditLogRepository;
        _paymentService = paymentService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<CheckoutSessionResponse>> Handle(
        CreateCheckoutSessionCommand command,
        CancellationToken cancellationToken)
    {
        var reservation = await _stockReservationRepository.GetByIdAsync(
            command.ReservationId,
            cancellationToken);

        if (reservation is null)
        {
            return Result<CheckoutSessionResponse>.NotFound("Reservation not found.");
        }

        if (_currentUser.StoreId != reservation.RequestingStoreId)
        {
            return Result<CheckoutSessionResponse>.Forbidden(
                "Only the requesting store can pay for a reservation.");
        }

        if (reservation.Status != ReservationStatus.Accepted)
        {
            return Result<CheckoutSessionResponse>.Conflict(
                "A reservation can only be paid once the owning store has accepted it.");
        }

        var existing = await _paymentRepository.GetByReservationIdAsync(
            reservation.Id,
            cancellationToken);

        if (existing is not null)
        {
            return existing.Status switch
            {
                PaymentStatus.Paid => Result<CheckoutSessionResponse>.Conflict(
                    "This reservation has already been paid."),
                PaymentStatus.Pending when !string.IsNullOrWhiteSpace(existing.CheckoutUrl) =>
                    Result<CheckoutSessionResponse>.Success(
                        new CheckoutSessionResponse(existing.StripeSessionId, existing.CheckoutUrl)),
                _ => await CreateReplacementAsync(reservation, existing, cancellationToken),
            };
        }

        return await CreateReplacementAsync(reservation, null, cancellationToken);
    }

    private async Task<Result<CheckoutSessionResponse>> CreateReplacementAsync(
        StockReservation reservation,
        ReservationPayment? previous,
        CancellationToken cancellationToken)
    {
        var ownerStore = await _storeRepository.GetByIdAsync(
            reservation.OwningStoreId,
            cancellationToken);

        if (ownerStore is null || string.IsNullOrWhiteSpace(ownerStore.StripeConnectAccountId))
        {
            await RecordOwnerNotOnboardedAsync(reservation, cancellationToken);

            return Result<CheckoutSessionResponse>.Conflict(
                "The owning store has not connected a payout account yet.");
        }

        var amount = reservation.Quantity * reservation.UnitPrice;

        CheckoutSessionResponse session;

        try
        {
            session = await _paymentService.CreateCheckoutSessionAsync(
                ownerStore.StripeConnectAccountId,
                reservation.Id,
                amount,
                _paymentService.DefaultCurrency,
                cancellationToken);
        }
        catch (OwnerNotOnboardedException ex)
        {
            _logger.LogWarning(
                ex,
                "Checkout skipped for reservation {ReservationId}: owner not onboarded.",
                reservation.Id);

            await RecordOwnerNotOnboardedAsync(reservation, cancellationToken);

            return Result<CheckoutSessionResponse>.Conflict(
                "The owning store has not connected a payout account yet.");
        }

        await using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);

        if (previous is null)
        {
            await _paymentRepository.AddAsync(
                new ReservationPayment(
                    reservation.Id,
                    session.SessionId,
                    ownerStore.StripeConnectAccountId,
                    amount,
                    _paymentService.DefaultCurrency,
                    session.CheckoutUrl),
                cancellationToken);
        }
        else
        {
            previous.ReopenForRetry(session.SessionId, session.CheckoutUrl);
            _paymentRepository.Update(previous);
        }

        await _auditLogRepository.AddAsync(new AuditLog(
            AuditActions.EntityType,
            reservation.Id,
            AuditActions.CheckoutCreated,
            _currentUser.StoreId,
            metadata: null,
            _clock.UtcNow), cancellationToken);

        await _paymentRepository.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return Result<CheckoutSessionResponse>.Success(session);
    }

    private async Task RecordOwnerNotOnboardedAsync(
        StockReservation reservation,
        CancellationToken cancellationToken)
    {
        await _auditLogRepository.AddAsync(new AuditLog(
            AuditActions.EntityType,
            reservation.Id,
            AuditActions.OwnerNotOnboarded,
            _currentUser.StoreId,
            metadata: null,
            _clock.UtcNow), cancellationToken);

        await _auditLogRepository.SaveChangesAsync(cancellationToken);
    }
}