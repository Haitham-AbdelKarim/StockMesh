using Application.Abstractions.Options;
using Application.Common.Models;
using Application.Features.Reservations.Commands.ReserveStock;
using Domain.Entities;
using Domain.Enums;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using StockMesh.Application.UnitTests.Fakes;

namespace StockMesh.Application.UnitTests.Features.Reservations;

public class ReserveStockCommandHandlerTests
{
    private static readonly Guid OwnerStoreId = Guid.NewGuid();
    private static readonly Guid RequesterStoreId = Guid.NewGuid();

    private static readonly Product Product =
        new("Paracetamol 500mg", VerticalCategory.Pharmacy, "Panadol");

    private static readonly FakeDateTimeProvider Clock = new();

    [Fact]
    public async Task Handle_ValidReservation_ReservesQuantityAndCreatesPendingReservation()
    {
        var batch = CreateSharedBatch(5);
        var batchRepository = new FakeInventoryBatchRepository(batch);
        var reservationRepository = new FakeStockReservationRepository();
        var auditRepository = new FakeAuditLogRepository();
        var unitOfWork = new FakeUnitOfWork();
        var handler = CreateHandler(
            new FakeReservationLockService(),
            batchRepository,
            reservationRepository,
            auditRepository,
            unitOfWork);

        var result = await handler.Handle(
            new ReserveStockCommand(batch.Id, 3, DistanceKm: 4.5m),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        batch.SharedQuantity.Should().Be(2);

        var reservation = reservationRepository.All.Should().ContainSingle().Subject;
        reservation.RequestingStoreId.Should().Be(RequesterStoreId);
        reservation.OwningStoreId.Should().Be(OwnerStoreId);
        reservation.Quantity.Should().Be(3);
        reservation.UnitPrice.Should().Be(10m);
        reservation.DistanceKm.Should().Be(4.5m);
        reservation.Status.Should().Be(ReservationStatus.Pending);
        reservation.HoldExpiresAt.Should().Be(Clock.UtcNow.AddMinutes(15));

        result.Value.Should().NotBeNull();
        result.Value!.Id.Should().Be(reservation.Id);
        result.Value.Status.Should().Be(ReservationStatus.Pending);
        result.Value.UnitPrice.Should().Be(10m);

        auditRepository.Entries.Should().ContainSingle(
            e => e.Action == "reservation.created" && e.EntityId == reservation.Id);
        unitOfWork.Transactions.Should().ContainSingle().Subject.Committed.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WhenBatchDoesNotExist_ReturnsNotFound_AndReleasesLock()
    {
        var unitOfWork = new FakeUnitOfWork();
        var holder = new FakeReservationLockService();
        var handler = CreateHandler(
            holder,
            new FakeInventoryBatchRepository(),
            new FakeStockReservationRepository(),
            new FakeAuditLogRepository(),
            unitOfWork);

        var result = await handler.Handle(
            new ReserveStockCommand(Guid.NewGuid(), 1),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Kind.Should().Be(FailureKind.NotFound);
        holder.ReleaseCount.Should().Be(1);
        unitOfWork.Transactions.Should().ContainSingle().Subject.RolledBack.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WhenReservingOwnBatch_ReturnsBadRequest()
    {
        var batch = CreateSharedBatch(5);
        var reservationRepository = new FakeStockReservationRepository();
        var handler = CreateHandler(
            new FakeReservationLockService(),
            new FakeInventoryBatchRepository(batch),
            reservationRepository,
            new FakeAuditLogRepository(),
            new FakeUnitOfWork(),
            currentStoreId: OwnerStoreId);

        var result = await handler.Handle(
            new ReserveStockCommand(batch.Id, 1),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Kind.Should().Be(FailureKind.BadRequest);
        batch.SharedQuantity.Should().Be(5);
        reservationRepository.All.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WhenQuantityExceedsSharedPool_ReturnsConflict()
    {
        var batch = CreateSharedBatch(2);
        var reservationRepository = new FakeStockReservationRepository();
        var unitOfWork = new FakeUnitOfWork();
        var handler = CreateHandler(
            new FakeReservationLockService(),
            new FakeInventoryBatchRepository(batch),
            reservationRepository,
            new FakeAuditLogRepository(),
            unitOfWork);

        var result = await handler.Handle(
            new ReserveStockCommand(batch.Id, 5),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Kind.Should().Be(FailureKind.Conflict);
        batch.SharedQuantity.Should().Be(2);
        reservationRepository.All.Should().BeEmpty();
        unitOfWork.Transactions.Should().ContainSingle().Subject.RolledBack.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WhenLockUnavailable_ReturnsConflict_AndDoesNotReserve()
    {
        var batch = CreateSharedBatch(5);
        var reservationRepository = new FakeStockReservationRepository();
        var holder = new FakeReservationLockService(acquireResult: false);
        var handler = CreateHandler(
            holder,
            new FakeInventoryBatchRepository(batch),
            reservationRepository,
            new FakeAuditLogRepository(),
            new FakeUnitOfWork());

        var result = await handler.Handle(
            new ReserveStockCommand(batch.Id, 1),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Kind.Should().Be(FailureKind.Conflict);
        batch.SharedQuantity.Should().Be(5);
        reservationRepository.All.Should().BeEmpty();
        holder.ReleaseCount.Should().Be(0);
    }

    [Fact]
    public async Task Handle_WhenSaveThrowsMidOperation_ReleasesLock()
    {
        var batch = CreateSharedBatch(5);
        var holder = new FakeReservationLockService();
        var reservationRepository = new FakeStockReservationRepository
        {
            SaveException = new InvalidOperationException("boom")
        };
        var handler = CreateHandler(
            holder,
            new FakeInventoryBatchRepository(batch),
            reservationRepository,
            new FakeAuditLogRepository(),
            new FakeUnitOfWork());

        var act = async () => await handler.Handle(
            new ReserveStockCommand(batch.Id, 1),
            CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
        holder.ReleaseCount.Should().Be(1);
        holder.AcquireCount.Should().Be(1);
    }

    private static InventoryBatch CreateSharedBatch(int sharedQuantity)
    {
        var batch = new InventoryBatch(OwnerStoreId, Product.Id, 20, 5m, 10m);
        batch.MarkAsShared(sharedQuantity);

        return batch;
    }

    private static ReserveStockCommandHandler CreateHandler(
        FakeReservationLockService lockService,
        FakeInventoryBatchRepository batchRepository,
        FakeStockReservationRepository reservationRepository,
        FakeAuditLogRepository auditRepository,
        FakeUnitOfWork unitOfWork,
        Guid? currentStoreId = null)
    {
        return new ReserveStockCommandHandler(
            new FakeCurrentUser { StoreId = currentStoreId ?? RequesterStoreId },
            Clock,
            batchRepository,
            reservationRepository,
            auditRepository,
            lockService,
            unitOfWork,
            new ReservationOptions(),
            NullLogger<ReserveStockCommandHandler>.Instance);
    }
}