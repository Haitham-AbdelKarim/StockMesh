using Application.Abstractions.Services;
using Application.Common.Models;
using Application.Features.Payments.Commands.ProcessStripeWebhook;
using Application.Features.Reservations.Notifications;
using Domain.Entities;
using Domain.Enums;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using StockMesh.Application.UnitTests.Fakes;

namespace StockMesh.Application.UnitTests.Features.Payments;

public class ProcessStripeWebhookCommandHandlerTests
{
    private static readonly FakeDateTimeProvider Clock = new();

    [Fact]
    public async Task Handle_WithInvalidSignature_ReturnsBadRequest()
    {
        var handler = CreateHandler(
            new FakeProcessedStripeEventRepository(),
            new FakeReservationPaymentRepository(),
            new FakeStockReservationRepository(),
            verifier: new FakeStripeWebhookVerifier());

        var result = await handler.Handle(
            new ProcessStripeWebhookCommand("{}", "bad"),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Kind.Should().Be(FailureKind.BadRequest);
    }

    [Fact]
    public async Task Handle_WithUnknownEventType_ReturnsIgnoredWithoutRecording()
    {
        var eventRepository = new FakeProcessedStripeEventRepository();
        var verifier = new FakeStripeWebhookVerifier
        {
            VerifyFactory = (payload, signature) => new StripePaymentEvent(
                "evt_unknown", "customer.created", null, null),
        };
        var handler = CreateHandler(eventRepository, new FakeReservationPaymentRepository(),
            new FakeStockReservationRepository(), verifier: verifier);

        var result = await handler.Handle(
            new ProcessStripeWebhookCommand("{}", "sig"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(WebhookOutcome.Ignored);
        eventRepository.All.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WithDuplicateEventId_ReturnsIgnored()
    {
        var eventRepository = new FakeProcessedStripeEventRepository();
        await eventRepository.AddAsync(new ProcessedStripeEvent("evt_dup"), CancellationToken.None);
        var verifier = CompletedVerifier("evt_dup", Guid.NewGuid());
        var handler = CreateHandler(eventRepository, new FakeReservationPaymentRepository(),
            new FakeStockReservationRepository(), verifier: verifier);

        var result = await handler.Handle(
            new ProcessStripeWebhookCommand("{}", "sig"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(WebhookOutcome.Ignored);
    }

    [Fact]
    public async Task Handle_OnCompleted_MarksPaidResolvesSuccessAndPublishesLedgerNotification()
    {
        var reservation = CreateAcceptedReservation(2);
        var payment = new ReservationPayment(
            reservation.Id, "cs_test_123", "acct_test_123", 20m, "usd");
        var paymentRepository = new FakeReservationPaymentRepository(payment);
        var eventRepository = new FakeProcessedStripeEventRepository();
        var auditRepository = new FakeAuditLogRepository();
        var mediator = new FakeMediator();
        var verifier = CompletedVerifier("evt_paid", reservation.Id);
        var handler = CreateHandler(
            eventRepository, paymentRepository,
            new FakeStockReservationRepository(reservation),
            verifier, mediator, auditRepository);

        var result = await handler.Handle(
            new ProcessStripeWebhookCommand("{}", "sig"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(WebhookOutcome.Paid);
        payment.Status.Should().Be(PaymentStatus.Paid);
        reservation.Status.Should().Be(ReservationStatus.Success);
        eventRepository.All.Should().ContainSingle(e => e.EventId == "evt_paid");
        auditRepository.Entries.Should().ContainSingle(
            e => e.Action == "payment.succeeded" && e.EntityId == reservation.Id);
        mediator.Published.Should().ContainSingle()
            .Which.Should().BeOfType<ReservationResolvedSuccessfullyNotification>()
            .Which.ReservationId.Should().Be(reservation.Id);
    }

    [Fact]
    public async Task Handle_OnCompletedForNonAcceptedReservation_RecordsPaymentWithoutResolving()
    {
        var reservation = new StockReservation(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 2, 10m,
            holdExpiresAt: Clock.UtcNow.AddMinutes(15));
        reservation.Resolve(ReservationStatus.Cancelled);
        var payment = new ReservationPayment(
            reservation.Id, "cs_test_123", "acct_test_123", 20m, "usd");
        var paymentRepository = new FakeReservationPaymentRepository(payment);
        var mediator = new FakeMediator();
        var verifier = CompletedVerifier("evt_late", reservation.Id);
        var handler = CreateHandler(
            new FakeProcessedStripeEventRepository(), paymentRepository,
            new FakeStockReservationRepository(reservation),
            verifier, mediator);

        var result = await handler.Handle(
            new ProcessStripeWebhookCommand("{}", "sig"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(WebhookOutcome.Paid);
        payment.Status.Should().Be(PaymentStatus.Paid);
        reservation.Status.Should().Be(ReservationStatus.Cancelled);
        mediator.Published.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_OnExpired_MarksPaymentFailedAndKeepsReservationAccepted()
    {
        var reservation = CreateAcceptedReservation(2);
        var payment = new ReservationPayment(
            reservation.Id, "cs_test_123", "acct_test_123", 20m, "usd");
        var paymentRepository = new FakeReservationPaymentRepository(payment);
        var auditRepository = new FakeAuditLogRepository();
        var mediator = new FakeMediator();
        var verifier = new FakeStripeWebhookVerifier
        {
            VerifyFactory = (payload, signature) => new StripePaymentEvent(
                "evt_expired", "checkout.session.expired", reservation.Id, "cs_test_123"),
        };
        var handler = CreateHandler(
            new FakeProcessedStripeEventRepository(), paymentRepository,
            new FakeStockReservationRepository(reservation),
            verifier, mediator, auditRepository);

        var result = await handler.Handle(
            new ProcessStripeWebhookCommand("{}", "sig"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(WebhookOutcome.Failed);
        payment.Status.Should().Be(PaymentStatus.Failed);
        reservation.Status.Should().Be(ReservationStatus.Accepted);
        auditRepository.Entries.Should().ContainSingle(
            e => e.Action == "payment.failed" && e.EntityId == reservation.Id);
        mediator.Published.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WhenSaveFailsWithConcurrencyViolation_ReturnsIgnored()
    {
        var reservation = CreateAcceptedReservation(2);
        var payment = new ReservationPayment(
            reservation.Id, "cs_test_123", "acct_test_123", 20m, "usd");
        var paymentRepository = new FakeReservationPaymentRepository(payment)
        {
            SaveException = new Microsoft.EntityFrameworkCore.DbUpdateException("Unique violation."),
        };
        var verifier = CompletedVerifier("evt_race", reservation.Id);
        var unitOfWork = new FakeUnitOfWork();
        var handler = CreateHandler(
            new FakeProcessedStripeEventRepository(), paymentRepository,
            new FakeStockReservationRepository(reservation),
            verifier: verifier,
            unitOfWork: unitOfWork);

        var result = await handler.Handle(
            new ProcessStripeWebhookCommand("{}", "sig"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(WebhookOutcome.Ignored);
        unitOfWork.Transactions.Should().ContainSingle().Subject.RolledBack.Should().BeTrue();
    }

    private static StockReservation CreateAcceptedReservation(int quantity)
    {
        var reservation = new StockReservation(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), quantity, 10m,
            holdExpiresAt: Clock.UtcNow.AddMinutes(15));
        reservation.Resolve(ReservationStatus.Accepted);

        return reservation;
    }

    private static FakeStripeWebhookVerifier CompletedVerifier(string eventId, Guid reservationId)
    {
        return new FakeStripeWebhookVerifier
        {
            VerifyFactory = (payload, signature) => new StripePaymentEvent(
                eventId, "checkout.session.completed", reservationId, "cs_test_123"),
        };
    }

    private static ProcessStripeWebhookCommandHandler CreateHandler(
        FakeProcessedStripeEventRepository eventRepository,
        FakeReservationPaymentRepository paymentRepository,
        FakeStockReservationRepository reservationRepository,
        FakeStripeWebhookVerifier? verifier = null,
        FakeMediator? mediator = null,
        FakeAuditLogRepository? auditRepository = null,
        FakeUnitOfWork? unitOfWork = null)
    {
        return new ProcessStripeWebhookCommandHandler(
            Clock,
            eventRepository,
            paymentRepository,
            reservationRepository,
            auditRepository ?? new FakeAuditLogRepository(),
            verifier ?? new FakeStripeWebhookVerifier(),
            unitOfWork ?? new FakeUnitOfWork(),
            mediator ?? new FakeMediator(),
            NullLogger<ProcessStripeWebhookCommandHandler>.Instance);
    }
}