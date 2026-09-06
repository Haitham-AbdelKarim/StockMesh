using Application.Features.Reservations.Services;
using Domain.Entities;
using Domain.Enums;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using StockMesh.Application.UnitTests.Fakes;

namespace StockMesh.Application.UnitTests.Features.Reservations;

public class ReservationExpiryProcessorTests
{
    private static readonly Guid OwnerStoreId = Guid.NewGuid();
    private static readonly Guid RequesterStoreId = Guid.NewGuid();

    private static readonly Product Product =
        new("Paracetamol 500mg", VerticalCategory.Pharmacy, "Panadol");

    private static readonly FakeDateTimeProvider Clock = new();

    [Fact]
    public async Task RunAsync_ReclaimsExpiredHold_RestoresSharedAndWritesAudit()
    {
        var now = Clock.UtcNow;
        var batch = CreateSharedBatch(5);
        var expired = CreatePendingReservation(batch.Id, 2, now.AddMinutes(-1));
        var notExpired = CreatePendingReservation(batch.Id, 1, now.AddMinutes(1));
        var auditRepository = new FakeAuditLogRepository();
        var unitOfWork = new FakeUnitOfWork();
        var processor = CreateProcessor(
            new FakeStockReservationRepository(expired, notExpired),
            new FakeInventoryBatchRepository(batch),
            auditRepository,
            unitOfWork);

        await processor.RunAsync(CancellationToken.None);

        expired.Status.Should().Be(ReservationStatus.Cancelled);
        batch.SharedQuantity.Should().Be(7);

        notExpired.Status.Should().Be(ReservationStatus.Pending);

        auditRepository.Entries.Should().ContainSingle(
            e => e.Action == "reservation.expired" && e.EntityId == expired.Id);
        auditRepository.Entries.Single().CreatedAt.Should().Be(now);
        unitOfWork.Transactions.Should().ContainSingle().Subject.Committed.Should().BeTrue();
    }

    [Fact]
    public async Task RunAsync_DoesNotTouchNonPendingReservations()
    {
        var now = Clock.UtcNow;
        var batch = CreateSharedBatch(5);
        var alreadyResolved = CreatePendingReservation(batch.Id, 2, now.AddMinutes(-1));
        alreadyResolved.Resolve(ReservationStatus.Cancelled);
        var auditRepository = new FakeAuditLogRepository();
        var processor = CreateProcessor(
            new FakeStockReservationRepository(alreadyResolved),
            new FakeInventoryBatchRepository(batch),
            auditRepository,
            new FakeUnitOfWork());

        await processor.RunAsync(CancellationToken.None);

        alreadyResolved.Status.Should().Be(ReservationStatus.Cancelled);
        auditRepository.Entries.Should().BeEmpty();
    }

    private static InventoryBatch CreateSharedBatch(int sharedQuantity)
    {
        var batch = new InventoryBatch(OwnerStoreId, Product.Id, 20, 5m, 10m);
        batch.MarkAsShared(sharedQuantity);

        return batch;
    }

    private static StockReservation CreatePendingReservation(
        Guid batchId,
        int quantity,
        DateTime holdExpiresAt)
    {
        return new StockReservation(
            batchId,
            RequesterStoreId,
            OwnerStoreId,
            quantity,
            10m,
            holdExpiresAt: holdExpiresAt);
    }

    private static ReservationExpiryProcessor CreateProcessor(
        FakeStockReservationRepository reservationRepository,
        FakeInventoryBatchRepository batchRepository,
        FakeAuditLogRepository auditRepository,
        FakeUnitOfWork unitOfWork)
    {
        return new ReservationExpiryProcessor(
            Clock,
            reservationRepository,
            batchRepository,
            auditRepository,
            unitOfWork,
            NullLogger<ReservationExpiryProcessor>.Instance);
    }
}