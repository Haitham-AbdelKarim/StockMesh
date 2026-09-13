using Application.Common.Models;
using Application.DTOs.Reservations;
using Application.Features.Reservations.Commands.CreateCheckoutSession;
using Domain.Entities;
using Domain.Enums;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using StockMesh.Application.UnitTests.Fakes;

namespace StockMesh.Application.UnitTests.Features.Reservations;

public class CreateCheckoutSessionCommandHandlerTests
{
    private static readonly FakeDateTimeProvider Clock = new();

    [Fact]
    public async Task Handle_WhenReservationDoesNotExist_ReturnsNotFound()
    {
        var handler = CreateHandler(
            new FakeStockReservationRepository(),
            new FakeStoreRepository(),
            new FakeReservationPaymentRepository());

        var result = await handler.Handle(
            new CreateCheckoutSessionCommand(Guid.NewGuid()),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Kind.Should().Be(FailureKind.NotFound);
    }

    [Fact]
    public async Task Handle_WhenCallerIsNotRequester_ReturnsForbidden()
    {
        var ownerStore = CreateOnboardedStore();
        var otherStore = CreateStore("Other");
        var reservation = CreateAcceptedReservation(
            Guid.NewGuid(), Guid.NewGuid(), ownerStore.Id, 2);
        var handler = CreateHandler(
            new FakeStockReservationRepository(reservation),
            new FakeStoreRepository(ownerStore, otherStore),
            new FakeReservationPaymentRepository(),
            currentStoreId: otherStore.Id);

        var result = await handler.Handle(
            new CreateCheckoutSessionCommand(reservation.Id),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Kind.Should().Be(FailureKind.Forbidden);
    }

    [Fact]
    public async Task Handle_WhenReservationNotAccepted_ReturnsConflict()
    {
        var ownerStore = CreateOnboardedStore();
        var requesterStore = CreateStore("Requester");
        var reservation = new StockReservation(
            Guid.NewGuid(), requesterStore.Id, ownerStore.Id, 2, 10m,
            holdExpiresAt: Clock.UtcNow.AddMinutes(15));
        var paymentService = new FakePaymentService();
        var handler = CreateHandler(
            new FakeStockReservationRepository(reservation),
            new FakeStoreRepository(ownerStore, requesterStore),
            new FakeReservationPaymentRepository(),
            paymentService: paymentService,
            currentStoreId: requesterStore.Id);

        var result = await handler.Handle(
            new CreateCheckoutSessionCommand(reservation.Id),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Kind.Should().Be(FailureKind.Conflict);
        paymentService.SessionCalls.Should().Be(0);
    }

    [Fact]
    public async Task Handle_OnAccepted_CreatesSessionWithOwnerDestination()
    {
        var ownerStore = CreateOnboardedStore();
        var requesterStore = CreateStore("Requester");
        var reservation = CreateAcceptedReservation(
            Guid.NewGuid(), requesterStore.Id, ownerStore.Id, 3);
        var paymentService = new FakePaymentService();
        var paymentRepository = new FakeReservationPaymentRepository();
        var auditRepository = new FakeAuditLogRepository();
        var handler = CreateHandler(
            new FakeStockReservationRepository(reservation),
            new FakeStoreRepository(ownerStore, requesterStore),
            paymentRepository,
            paymentService: paymentService,
            auditRepository: auditRepository,
            currentStoreId: requesterStore.Id);

        var result = await handler.Handle(
            new CreateCheckoutSessionCommand(reservation.Id),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.SessionId.Should().Be("cs_test_123");
        paymentService.SessionCalls.Should().Be(1);
        paymentService.LastDestination.Should().Be("acct_test_123");

        var payment = paymentRepository.All.Should().ContainSingle().Subject;
        payment.ReservationId.Should().Be(reservation.Id);
        payment.Amount.Should().Be(30m);
        payment.Status.Should().Be(PaymentStatus.Pending);
        auditRepository.Entries.Should().ContainSingle(
            e => e.Action == "payment.checkout.created" && e.EntityId == reservation.Id);
    }

    [Fact]
    public async Task Handle_WhenPendingPaymentExists_ReturnsExistingUrlWithoutNewSession()
    {
        var ownerStore = CreateOnboardedStore();
        var requesterStore = CreateStore("Requester");
        var reservation = CreateAcceptedReservation(
            Guid.NewGuid(), requesterStore.Id, ownerStore.Id, 2);
        var existing = new ReservationPayment(
            reservation.Id, "cs_existing", "acct_test_123", 20m, "usd",
            "https://checkout.stripe.com/pay/existing");
        var paymentService = new FakePaymentService();
        var handler = CreateHandler(
            new FakeStockReservationRepository(reservation),
            new FakeStoreRepository(ownerStore, requesterStore),
            new FakeReservationPaymentRepository(existing),
            paymentService: paymentService,
            currentStoreId: requesterStore.Id);

        var result = await handler.Handle(
            new CreateCheckoutSessionCommand(reservation.Id),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.CheckoutUrl.Should().Be("https://checkout.stripe.com/pay/existing");
        paymentService.SessionCalls.Should().Be(0);
    }

    [Fact]
    public async Task Handle_WhenAlreadyPaid_ReturnsConflict()
    {
        var ownerStore = CreateOnboardedStore();
        var requesterStore = CreateStore("Requester");
        var reservation = CreateAcceptedReservation(
            Guid.NewGuid(), requesterStore.Id, ownerStore.Id, 2);
        var paid = new ReservationPayment(
            reservation.Id, "cs_paid", "acct_test_123", 20m, "usd");
        paid.MarkPaid();
        var handler = CreateHandler(
            new FakeStockReservationRepository(reservation),
            new FakeStoreRepository(ownerStore, requesterStore),
            new FakeReservationPaymentRepository(paid),
            currentStoreId: requesterStore.Id);

        var result = await handler.Handle(
            new CreateCheckoutSessionCommand(reservation.Id),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Kind.Should().Be(FailureKind.Conflict);
    }

    [Fact]
    public async Task Handle_WhenFailedPaymentExists_CreatesReplacementSession()
    {
        var ownerStore = CreateOnboardedStore();
        var requesterStore = CreateStore("Requester");
        var reservation = CreateAcceptedReservation(
            Guid.NewGuid(), requesterStore.Id, ownerStore.Id, 2);
        var failed = new ReservationPayment(
            reservation.Id, "cs_old", "acct_test_123", 20m, "usd");
        failed.MarkFailed();
        var paymentService = new FakePaymentService();
        var paymentRepository = new FakeReservationPaymentRepository(failed);
        var handler = CreateHandler(
            new FakeStockReservationRepository(reservation),
            new FakeStoreRepository(ownerStore, requesterStore),
            paymentRepository,
            paymentService: paymentService,
            currentStoreId: requesterStore.Id);

        var result = await handler.Handle(
            new CreateCheckoutSessionCommand(reservation.Id),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        paymentService.SessionCalls.Should().Be(1);
        paymentRepository.All.Should().ContainSingle();
        failed.Status.Should().Be(PaymentStatus.Pending);
        failed.StripeSessionId.Should().Be("cs_test_123");
    }

    [Fact]
    public async Task Handle_WhenOwnerNotOnboarded_ReturnsConflictAndAudits()
    {
        var ownerStore = CreateStore("Owner");
        var requesterStore = CreateStore("Requester");
        var reservation = CreateAcceptedReservation(
            Guid.NewGuid(), requesterStore.Id, ownerStore.Id, 2);
        var paymentService = new FakePaymentService();
        var paymentRepository = new FakeReservationPaymentRepository();
        var auditRepository = new FakeAuditLogRepository();
        var handler = CreateHandler(
            new FakeStockReservationRepository(reservation),
            new FakeStoreRepository(ownerStore, requesterStore),
            paymentRepository,
            paymentService: paymentService,
            auditRepository: auditRepository,
            currentStoreId: requesterStore.Id);

        var result = await handler.Handle(
            new CreateCheckoutSessionCommand(reservation.Id),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Kind.Should().Be(FailureKind.Conflict);
        paymentService.SessionCalls.Should().Be(0);
        paymentRepository.All.Should().BeEmpty();
        auditRepository.Entries.Should().ContainSingle(
            e => e.Action == "payment.owner-not-onboarded" && e.EntityId == reservation.Id);
    }

    private static Store CreateStore(string name)
    {
        return new Store(name, VerticalCategory.Pharmacy, 30.0, 31.0);
    }

    private static Store CreateOnboardedStore()
    {
        var store = CreateStore("Owner");
        store.ConnectStripeAccount("acct_test_123");
        store.SetPayoutStatus(true);

        return store;
    }

    private static StockReservation CreateAcceptedReservation(
        Guid batchId, Guid requesterId, Guid ownerId, int quantity)
    {
        var reservation = new StockReservation(
            batchId, requesterId, ownerId, quantity, 10m,
            holdExpiresAt: Clock.UtcNow.AddMinutes(15));
        reservation.Resolve(ReservationStatus.Accepted);

        return reservation;
    }

    private static CreateCheckoutSessionCommandHandler CreateHandler(
        FakeStockReservationRepository reservationRepository,
        FakeStoreRepository storeRepository,
        FakeReservationPaymentRepository paymentRepository,
        FakePaymentService? paymentService = null,
        FakeAuditLogRepository? auditRepository = null,
        Guid? currentStoreId = null)
    {
        return new CreateCheckoutSessionCommandHandler(
            new FakeCurrentUser { StoreId = currentStoreId ?? Guid.NewGuid() },
            Clock,
            reservationRepository,
            storeRepository,
            paymentRepository,
            auditRepository ?? new FakeAuditLogRepository(),
            paymentService ?? new FakePaymentService(),
            new FakeUnitOfWork(),
            NullLogger<CreateCheckoutSessionCommandHandler>.Instance);
    }
}