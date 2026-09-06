using Application.Common.Models;
using Application.Features.Reservations.Commands.ResolveReservation;
using Application.Features.Reservations.Notifications;
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
            new FakeUnitOfWork(),
            new FakeMediator());

        var result = await handler.Handle(
            new ResolveReservationCommand(Guid.NewGuid(), ReservationStatus.Success),
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
            new FakeMediator(),
            currentStoreId: ThirdStoreId);

        var result = await handler.Handle(
            new ResolveReservationCommand(reservation.Id, ReservationStatus.Success),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Kind.Should().Be(FailureKind.Forbidden);
    }

    [Fact]
    public async Task Handle_SuccessByRequestingStore_SetsSuccessAndPublishesNotification()
    {
        var batch = CreateSharedBatch(5);
        var reservation = CreatePendingReservation(batch.Id, 2);
        var mediator = new FakeMediator();
        var unitOfWork = new FakeUnitOfWork();
        var auditRepository = new FakeAuditLogRepository();
        var handler = CreateHandler(
            new FakeStockReservationRepository(reservation),
            new FakeInventoryBatchRepository(batch),
            auditRepository,
            unitOfWork,
            mediator);

        var result = await handler.Handle(
            new ResolveReservationCommand(reservation.Id, ReservationStatus.Success),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be(ReservationStatus.Success);
        reservation.Status.Should().Be(ReservationStatus.Success);
        batch.SharedQuantity.Should().Be(5);

        mediator.Published.Should().ContainSingle()
            .Which.Should().BeOfType<ReservationResolvedSuccessfullyNotification>()
            .Which.ReservationId.Should().Be(reservation.Id);
        auditRepository.Entries.Should().ContainSingle(
            e => e.Action == "reservation.resolved.success" && e.EntityId == reservation.Id);
        unitOfWork.Transactions.Should().ContainSingle().Subject.Committed.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_SuccessByOwningStore_IsAllowed()
    {
        var batch = CreateSharedBatch(5);
        var reservation = CreatePendingReservation(batch.Id, 2);
        var handler = CreateHandler(
            new FakeStockReservationRepository(reservation),
            new FakeInventoryBatchRepository(batch),
            new FakeAuditLogRepository(),
            new FakeUnitOfWork(),
            new FakeMediator(),
            currentStoreId: OwnerStoreId);

        var result = await handler.Handle(
            new ResolveReservationCommand(reservation.Id, ReservationStatus.Success),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        reservation.Status.Should().Be(ReservationStatus.Success);
    }

    [Fact]
    public async Task Handle_Cancelled_ReturnsQuantityToSharedPool()
    {
        var batch = CreateSharedBatch(5);
        var reservation = CreatePendingReservation(batch.Id, 2);
        var mediator = new FakeMediator();
        var auditRepository = new FakeAuditLogRepository();
        var handler = CreateHandler(
            new FakeStockReservationRepository(reservation),
            new FakeInventoryBatchRepository(batch),
            auditRepository,
            new FakeUnitOfWork(),
            mediator);

        var result = await handler.Handle(
            new ResolveReservationCommand(reservation.Id, ReservationStatus.Cancelled),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be(ReservationStatus.Cancelled);
        batch.SharedQuantity.Should().Be(7);
        mediator.Published.Should().BeEmpty();
        auditRepository.Entries.Should().ContainSingle(
            e => e.Action == "reservation.resolved.cancelled" && e.EntityId == reservation.Id);
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
            new FakeMediator());

        await handler.Handle(
            new ResolveReservationCommand(reservation.Id, ReservationStatus.Success),
            CancellationToken.None);

        var result = await handler.Handle(
            new ResolveReservationCommand(reservation.Id, ReservationStatus.Cancelled),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Kind.Should().Be(FailureKind.Conflict);
        batch.SharedQuantity.Should().Be(5);
        reservation.Status.Should().Be(ReservationStatus.Success);
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
        FakeMediator mediator,
        Guid? currentStoreId = null)
    {
        return new ResolveReservationCommandHandler(
            new FakeCurrentUser { StoreId = currentStoreId ?? RequesterStoreId },
            Clock,
            reservationRepository,
            batchRepository,
            auditRepository,
            unitOfWork,
            mediator,
            NullLogger<ResolveReservationCommandHandler>.Instance);
    }
}