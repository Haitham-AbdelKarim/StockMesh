using Application.Features.Reservations.Notifications;
using Domain.Entities;
using Domain.Enums;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using StockMesh.Application.UnitTests.Fakes;

namespace StockMesh.Application.UnitTests.Features.Reservations;

public class ReservationResolvedSuccessfullyNotificationHandlerTests
{
    private static readonly Guid OwnerStoreId = Guid.NewGuid();
    private static readonly Guid RequesterStoreId = Guid.NewGuid();
    private static readonly FakeDateTimeProvider Clock = new();

    private static readonly Product Product =
        new("Paracetamol 500mg", VerticalCategory.Pharmacy, "Panadol");

    [Fact]
    public async Task Handle_OnSuccess_RecordsTransferAndRequesterBatch()
    {
        var ownerBatch = CreateSharedBatch(10);
        var reservation = CreateSuccessReservation(ownerBatch.Id, 3);
        var batchRepository = new FakeInventoryBatchRepository(ownerBatch);
        var movementRepository = new FakeStockMovementRepository();
        var handler = CreateHandler(
            new FakeStockReservationRepository(reservation),
            batchRepository,
            movementRepository);

        await handler.Handle(
            new ReservationResolvedSuccessfullyNotification(reservation.Id),
            CancellationToken.None);

        movementRepository.All.Should().HaveCount(2);

        movementRepository.All.Should().ContainSingle(m =>
            m.StoreId == OwnerStoreId
            && m.BatchId == ownerBatch.Id
            && m.MovementType == MovementType.NetworkTransferOut
            && m.Quantity == 3
            && m.RelatedStoreId == RequesterStoreId
            && m.UnitPrice == 10m
            && m.UnitCost == 5m);

        var incomingBatch = (await batchRepository.GetByProductAsync(Product.Id, CancellationToken.None))
            .SingleOrDefault(b => b.StoreId == RequesterStoreId);

        incomingBatch.Should().NotBeNull();
        incomingBatch!.QuantityRemaining.Should().Be(3);
        incomingBatch.UnitCost.Should().Be(10m);
        incomingBatch.UnitSalePrice.Should().Be(10m);

        movementRepository.All.Should().ContainSingle(m =>
            m.StoreId == RequesterStoreId
            && m.BatchId == incomingBatch.Id
            && m.MovementType == MovementType.NetworkTransferIn
            && m.Quantity == 3
            && m.RelatedStoreId == OwnerStoreId
            && m.UnitPrice == 10m);

        ownerBatch.QuantityRemaining.Should().Be(10);
        ownerBatch.SharedQuantity.Should().Be(10);
    }

    [Fact]
    public async Task Handle_WhenRequesterHasPreviousBatch_UsesItsSalePrice()
    {
        var ownerBatch = CreateSharedBatch(5);
        var reservation = CreateSuccessReservation(ownerBatch.Id, 2);
        var previousRequesterBatch =
            new InventoryBatch(RequesterStoreId, Product.Id, 4, 5m, 18m);
        var batchRepository = new FakeInventoryBatchRepository(ownerBatch, previousRequesterBatch);
        var movementRepository = new FakeStockMovementRepository();
        var handler = CreateHandler(
            new FakeStockReservationRepository(reservation),
            batchRepository,
            movementRepository);

        await handler.Handle(
            new ReservationResolvedSuccessfullyNotification(reservation.Id),
            CancellationToken.None);

        var incomingBatch = (await batchRepository.GetByProductAsync(Product.Id, CancellationToken.None))
            .Single(b => b.StoreId == RequesterStoreId && b.Id != previousRequesterBatch.Id);

        incomingBatch.UnitSalePrice.Should().Be(18m);
    }

    [Fact]
    public async Task Handle_WhenReservationMissing_RecordsNothing()
    {
        var movementRepository = new FakeStockMovementRepository();
        var handler = CreateHandler(
            new FakeStockReservationRepository(),
            new FakeInventoryBatchRepository(),
            movementRepository);

        await handler.Handle(
            new ReservationResolvedSuccessfullyNotification(Guid.NewGuid()),
            CancellationToken.None);

        movementRepository.All.Should().BeEmpty();
        movementRepository.SaveChangesCalled.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_WhenSaveFails_DoesNotThrow()
    {
        var ownerBatch = CreateSharedBatch(5);
        var reservation = CreateSuccessReservation(ownerBatch.Id, 2);
        var movementRepository = new FakeStockMovementRepository
        {
            SaveException = new InvalidOperationException("Database unavailable.")
        };
        var handler = CreateHandler(
            new FakeStockReservationRepository(reservation),
            new FakeInventoryBatchRepository(ownerBatch),
            movementRepository);

        var act = () => handler.Handle(
            new ReservationResolvedSuccessfullyNotification(reservation.Id),
            CancellationToken.None);

        await act.Should().NotThrowAsync();
    }

    private static InventoryBatch CreateSharedBatch(int sharedQuantity)
    {
        var batch = new InventoryBatch(OwnerStoreId, Product.Id, 20, 5m, 10m);
        batch.MarkAsShared(sharedQuantity);

        return batch;
    }

    private static StockReservation CreateSuccessReservation(Guid batchId, int quantity)
    {
        var reservation = new StockReservation(
            batchId,
            RequesterStoreId,
            OwnerStoreId,
            quantity,
            10m,
            holdExpiresAt: Clock.UtcNow.AddMinutes(15));

        reservation.Resolve(ReservationStatus.Success);

        return reservation;
    }

    private static ReservationResolvedSuccessfullyNotificationHandler CreateHandler(
        FakeStockReservationRepository reservationRepository,
        FakeInventoryBatchRepository batchRepository,
        FakeStockMovementRepository movementRepository)
    {
        return new ReservationResolvedSuccessfullyNotificationHandler(
            reservationRepository,
            batchRepository,
            movementRepository,
            Clock,
            NullLogger<ReservationResolvedSuccessfullyNotificationHandler>.Instance);
    }
}