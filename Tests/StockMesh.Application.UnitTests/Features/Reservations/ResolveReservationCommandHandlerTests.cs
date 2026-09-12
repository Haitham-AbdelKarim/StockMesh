using Application.Common.Models;
using Application.Features.Reservations.Commands.ResolveReservation;
using Domain.Entities;
using Domain.Enums;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using StockMesh.Application.UnitTests.Fakes;

namespace StockMesh.Application.UnitTests.Features.Reservations;

public class ResolveReservationCommandHandlerTests
{
    private static readonly Guid OwnerStoreId = Guid.NewGuid();
    private static readonly Guid RequesterStoreId = Guid.NewGuid();
    private static readonly Guid ThirdStoreId = Guid.NewGuid();

    private static readonly Product Product =
        new("Paracetamol 500mg", VerticalCategory.Pharmacy, "Panadol");

    private static readonly FakeDateTimeProvider Clock = new();

    [Fact]
    public async Task Handle_WhenReservationDoesNotExist_ReturnsNotFound()
    {
        var handler = CreateHandler(
            new FakeStockReservationRepository(),
            new FakeInventoryBatchRepository(),
            new FakeAuditLogRepository(),
            new FakeUnitOfWork());

        var result = await handler.Handle(
            new ResolveReservationCommand(Guid.NewGuid(), ReservationStatus.Accepted),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Kind.Should().Be(FailureKind.NotFound);
    }

    [Fact]
    public async Task Handle_WhenCallerIsNotAParty_ReturnsForbidden()
    {
        var batch = CreateSharedBatch(5);
        var reservation = CreatePendingReservation(batch.Id, 2);
        var handler = CreateHandler(
            new FakeStockReservationRepository(reservation),
            new FakeInventoryBatchRepository(batch),
            new FakeAuditLogRepository(),
            new FakeUnitOfWork(),
            currentStoreId: ThirdStoreId);

        var result = await handler.Handle(
            new ResolveReservationCommand(reservation.Id, ReservationStatus.Accepted),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Kind.Should().Be(FailureKind.Forbidden);
    }

    [Fact]
    public async Task Handle_AcceptByOwningStore_SetsAcceptedWithoutTouchingPool()
    {
        var batch = CreateSharedBatch(5);
        var reservation = CreatePendingReservation(batch.Id, 2);
        var unitOfWork = new FakeUnitOfWork();
        var auditRepository = new FakeAuditLogRepository();
        var handler = CreateHandler(
            new FakeStockReservationRepository(reservation),
            new FakeInventoryBatchRepository(batch),
            auditRepository,
            unitOfWork,
            currentStoreId: OwnerStoreId);

        var result = await handler.Handle(
            new ResolveReservationCommand(reservation.Id, ReservationStatus.Accepted),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be(ReservationStatus.Accepted);
        reservation.Status.Should().Be(ReservationStatus.Accepted);
        batch.SharedQuantity.Should().Be(5);
        auditRepository.Entries.Should().ContainSingle(
            e => e.Action == "reservation.accepted" && e.EntityId == reservation.Id);
        unitOfWork.Transactions.Should().ContainSingle().Subject.Committed.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_AcceptByRequestingStore_ReturnsForbidden()
    {
        var batch = CreateSharedBatch(5);
        var reservation = CreatePendingReservation(batch.Id, 2);
        var handler = CreateHandler(
            new FakeStockReservationRepository(reservation),
            new FakeInventoryBatchRepository(batch),
            new FakeAuditLogRepository(),
            new FakeUnitOfWork(),
            currentStoreId: RequesterStoreId);

        var result = await handler.Handle(
            new ResolveReservationCommand(reservation.Id, ReservationStatus.Accepted),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Kind.Should().Be(FailureKind.Forbidden);
        reservation.Status.Should().Be(ReservationStatus.Pending);
    }

    [Fact]
    public async Task Handle_SuccessDirectly_ReturnsBadRequest()
    {
        var batch = CreateSharedBatch(5);
        var reservation = CreatePendingReservation(batch.Id, 2);
        var handler = CreateHandler(
            new FakeStockReservationRepository(reservation),
            new FakeInventoryBatchRepository(batch),
            new FakeAuditLogRepository(),
            new FakeUnitOfWork(),
            currentStoreId: OwnerStoreId);

        var result = await handler.Handle(
            new ResolveReservationCommand(reservation.Id, ReservationStatus.Success),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Kind.Should().Be(FailureKind.BadRequest);
        reservation.Status.Should().Be(ReservationStatus.Pending);
    }

    [Fact]
    public async Task Handle_CancelledFromPending_ReturnsQuantityToSharedPool()
    {
        var batch = CreateSharedBatch(5);
        var reservation = CreatePendingReservation(batch.Id, 2);
        var auditRepository = new FakeAuditLogRepository();
        var handler = CreateHandler(
            new FakeStockReservationRepository(reservation),
            new FakeInventoryBatchRepository(batch),
            auditRepository,
            new FakeUnitOfWork());

        var result = await handler.Handle(
            new ResolveReservationCommand(reservation.Id, ReservationStatus.Cancelled),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be(ReservationStatus.Cancelled);
        batch.SharedQuantity.Should().Be(7);
        auditRepository.Entries.Should().ContainSingle(
            e => e.Action == "reservation.resolved.cancelled" && e.EntityId == reservation.Id);
    }

    [Fact]
    public async Task Handle_CancelledFromAccepted_ReturnsQuantityToSharedPool()
    {
        var batch = CreateSharedBatch(5);
        var reservation = CreatePendingReservation(batch.Id, 2);
        reservation.Resolve(ReservationStatus.Accepted);
        var handler = CreateHandler(
            new FakeStockReservationRepository(reservation),
            new FakeInventoryBatchRepository(batch),
            new FakeAuditLogRepository(),
            new FakeUnitOfWork(),
            currentStoreId: OwnerStoreId);

        var result = await handler.Handle(
            new ResolveReservationCommand(reservation.Id, ReservationStatus.Cancelled),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be(ReservationStatus.Cancelled);
        batch.SharedQuantity.Should().Be(7);
    }

    [Fact]
    public async Task Handle_WhenAlreadyResolved_ReturnsConflict()
    {
        var batch = CreateSharedBatch(5);
        var reservation = CreatePendingReservation(batch.Id, 2);
        var handler = CreateHandler(
            new FakeStockReservationRepository(reservation),
            new FakeInventoryBatchRepository(batch),
            new FakeAuditLogRepository(),
            new FakeUnitOfWork(),
            currentStoreId: OwnerStoreId);

        await handler.Handle(
            new ResolveReservationCommand(reservation.Id, ReservationStatus.Accepted),
            CancellationToken.None);

        var result = await handler.Handle(
            new ResolveReservationCommand(reservation.Id, ReservationStatus.Cancelled),
            CancellationToken.None);

        // Accepted -> Cancelled is legal, so resolve once more to prove terminality.
        result.IsSuccess.Should().BeTrue();

        var terminal = await handler.Handle(
            new ResolveReservationCommand(reservation.Id, ReservationStatus.Accepted),
            CancellationToken.None);

        terminal.IsSuccess.Should().BeFalse();
        terminal.Kind.Should().Be(FailureKind.Conflict);
        batch.SharedQuantity.Should().Be(7);
        reservation.Status.Should().Be(ReservationStatus.Cancelled);
    }

    private static InventoryBatch CreateSharedBatch(int sharedQuantity)
    {
        var batch = new InventoryBatch(OwnerStoreId, Product.Id, 20, 5m, 10m);
        batch.MarkAsShared(sharedQuantity);

        return batch;
    }

    private static StockReservation CreatePendingReservation(Guid batchId, int quantity)
    {
        return new StockReservation(
            batchId,
            RequesterStoreId,
            OwnerStoreId,
            quantity,
            10m,
            holdExpiresAt: Clock.UtcNow.AddMinutes(15));
    }

    private static ResolveReservationCommandHandler CreateHandler(
        FakeStockReservationRepository reservationRepository,
        FakeInventoryBatchRepository batchRepository,
        FakeAuditLogRepository auditRepository,
        FakeUnitOfWork unitOfWork,
        Guid? currentStoreId = null)
    {
        return new ResolveReservationCommandHandler(
            new FakeCurrentUser { StoreId = currentStoreId ?? RequesterStoreId },
            Clock,
            reservationRepository,
            batchRepository,
            new FakeReservationPaymentRepository(),
            auditRepository,
            unitOfWork,
            NullLogger<ResolveReservationCommandHandler>.Instance);
    }
}