using Application.Abstractions.Persistence;
using Application.Abstractions.Repositories;
using Application.Abstractions.Services;
using Application.Common.Models;
using Application.Exceptions;
using Application.Features.Reservations.Notifications;
using Domain.Entities;
using Domain.Enums;
using Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Application.Features.Payments.Commands.ProcessStripeWebhook;

public sealed class ProcessStripeWebhookCommandHandler :
    IRequestHandler<ProcessStripeWebhookCommand, Result<WebhookOutcome>>
{
    private static class AuditActions
    {
        public const string PaymentEntityType = "ReservationPayment";

        public const string PaymentSucceeded = "payment.succeeded";

        public const string PaymentFailed = "payment.failed";
    }

    private readonly IDateTimeProvider _clock;
    private readonly IProcessedStripeEventRepository _eventRepository;
    private readonly IReservationPaymentRepository _paymentRepository;
    private readonly IStockReservationRepository _stockReservationRepository;
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly IStripeWebhookVerifier _verifier;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMediator _mediator;
    private readonly ILogger<ProcessStripeWebhookCommandHandler> _logger;

    public ProcessStripeWebhookCommandHandler(
        IDateTimeProvider clock,
        IProcessedStripeEventRepository eventRepository,
        IReservationPaymentRepository paymentRepository,
        IStockReservationRepository stockReservationRepository,
        IAuditLogRepository auditLogRepository,
        IStripeWebhookVerifier verifier,
        IUnitOfWork unitOfWork,
        IMediator mediator,
        ILogger<ProcessStripeWebhookCommandHandler> logger)
    {
        _clock = clock;
        _eventRepository = eventRepository;
        _paymentRepository = paymentRepository;
        _stockReservationRepository = stockReservationRepository;
        _auditLogRepository = auditLogRepository;
        _verifier = verifier;
        _unitOfWork = unitOfWork;
        _mediator = mediator;
        _logger = logger;
    }

    public async Task<Result<WebhookOutcome>> Handle(
        ProcessStripeWebhookCommand command,
        CancellationToken cancellationToken)
    {
        StripePaymentEvent paymentEvent;

        try
        {
            paymentEvent = _verifier.Verify(command.Payload, command.SignatureHeader);
        }
        catch (InvalidWebhookSignatureException ex)
        {
            _logger.LogWarning(ex, "Rejected Stripe webhook with invalid signature.");

            return Result<WebhookOutcome>.BadRequest("Invalid webhook signature.");
        }

        if (paymentEvent.EventType is not ("checkout.session.completed" or "checkout.session.expired" or "payment_intent.payment_failed"))
        {
            return Result<WebhookOutcome>.Success(WebhookOutcome.Ignored);
        }

        if (await _eventRepository.ExistsAsync(paymentEvent.EventId, cancellationToken))
        {
            return Result<WebhookOutcome>.Success(WebhookOutcome.Ignored);
        }

        await using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);

        WebhookOutcome outcome;
        Guid? completedReservationId;

        try
        {
            await _eventRepository.AddAsync(
                new ProcessedStripeEvent(paymentEvent.EventId, _clock.UtcNow),
                cancellationToken);

            (outcome, completedReservationId) = await ApplyAsync(paymentEvent, cancellationToken);

            await _paymentRepository.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateException ex)
        {
            // Concurrent duplicate delivery: the unique EventId index rejects the
            // second insert. Roll back and treat as a duplicate.
            _logger.LogWarning(
                ex,
                "Duplicate Stripe webhook delivery {EventId} ignored.",
                paymentEvent.EventId);

            await transaction.RollbackAsync(cancellationToken);

            return Result<WebhookOutcome>.Success(WebhookOutcome.Ignored);
        }

        // Published after commit: the ledger handler opens its own transaction,
        // so it must never run inside this one.
        if (completedReservationId.HasValue)
        {
            await _mediator.Publish(
                new ReservationResolvedSuccessfullyNotification(completedReservationId.Value),
                cancellationToken);
        }

        return Result<WebhookOutcome>.Success(outcome);
    }

    private async Task<(WebhookOutcome Outcome, Guid? CompletedReservationId)> ApplyAsync(
        StripePaymentEvent paymentEvent,
        CancellationToken cancellationToken)
    {
        if (paymentEvent.ReservationId is null)
        {
            _logger.LogWarning(
                "Stripe webhook {EventId} ({EventType}) carries no reservation reference.",
                paymentEvent.EventId,
                paymentEvent.EventType);

            return (WebhookOutcome.Ignored, null);
        }

        var payment = await _paymentRepository.GetByReservationIdAsync(
            paymentEvent.ReservationId.Value,
            cancellationToken);

        if (payment is null)
        {
            _logger.LogWarning(
                "Stripe webhook {EventId} references unknown reservation {ReservationId}.",
                paymentEvent.EventId,
                paymentEvent.ReservationId);

            return (WebhookOutcome.Ignored, null);
        }

        if (paymentEvent.EventType == "checkout.session.completed")
        {
            return await ApplyPaymentSucceededAsync(payment, paymentEvent, cancellationToken);
        }

        if (payment.Status == PaymentStatus.Pending)
        {
            payment.MarkFailed();
            _paymentRepository.Update(payment);

            await AuditAsync(payment, AuditActions.PaymentFailed, cancellationToken);

            return (WebhookOutcome.Failed, null);
        }

        return (WebhookOutcome.Ignored, null);
    }

    private async Task<(WebhookOutcome Outcome, Guid? CompletedReservationId)> ApplyPaymentSucceededAsync(
        ReservationPayment payment,
        StripePaymentEvent paymentEvent,
        CancellationToken cancellationToken)
    {
        if (payment.Status == PaymentStatus.Pending)
        {
            payment.MarkPaid();
            _paymentRepository.Update(payment);

            await AuditAsync(payment, AuditActions.PaymentSucceeded, cancellationToken);
        }

        var reservation = await _stockReservationRepository.GetByIdAsync(
            payment.ReservationId,
            cancellationToken);

        if (reservation is null)
        {
            _logger.LogWarning(
                "Paid reservation {ReservationId} no longer exists.",
                payment.ReservationId);

            return (WebhookOutcome.Paid, null);
        }

        if (reservation.Status != ReservationStatus.Accepted)
        {
            // Money arrived but the reservation is no longer awaiting payment
            // (cancelled or expired after checkout). The payment is recorded;
            // unwinding it is a manual refund, not an automatic reversal.
            _logger.LogWarning(
                "Payment {PaymentId} succeeded for reservation {ReservationId} in status {Status}.",
                payment.Id,
                reservation.Id,
                reservation.Status);

            return (WebhookOutcome.Paid, null);
        }

        try
        {
            reservation.Resolve(ReservationStatus.Success);
        }
        catch (InvalidReservationTransitionException ex)
        {
            _logger.LogWarning(
                ex,
                "Reservation {ReservationId} could not transition to Success on payment.",
                reservation.Id);

            return (WebhookOutcome.Paid, null);
        }

        _stockReservationRepository.Update(reservation);

        return (WebhookOutcome.Paid, reservation.Id);
    }

    private Task AuditAsync(
        ReservationPayment payment,
        string action,
        CancellationToken cancellationToken)
    {
        return _auditLogRepository.AddAsync(
            new AuditLog(
                AuditActions.PaymentEntityType,
                payment.ReservationId,
                action,
                actorStoreId: null,
                metadata: null,
                _clock.UtcNow),
            cancellationToken);
    }
}