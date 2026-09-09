using Application.Features.MarketSignals.Services;
using Domain.Entities;
using Domain.Enums;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using StockMesh.Application.UnitTests.Fakes;

namespace StockMesh.Application.UnitTests.Features.MarketSignals;

public class MarketSignalAggregatorTests
{
    private static readonly Guid StoreAId = Guid.NewGuid();
    private static readonly Guid StoreBId = Guid.NewGuid();
    private static readonly Guid StoreCId = Guid.NewGuid();
    private static readonly Guid StoreDId = Guid.NewGuid();

    private static readonly Product GamingProduct =
        new("PlayStation 5 Console", VerticalCategory.Gaming, "Sony");

    private static readonly Product PharmacyProduct =
        new("Paracetamol 500mg", VerticalCategory.Pharmacy, "Panadol");

    private static readonly FakeDateTimeProvider Clock = new();

    [Fact]
    public async Task RunAsync_AggregatesReservationsAndTransfersAcrossStores()
    {
        var today = DateTime.UtcNow.Date;
        var noon = today.AddHours(12);

        var batchA = new InventoryBatch(StoreAId, GamingProduct.Id, 20, 5m, 10m);
        var batchB = new InventoryBatch(StoreBId, GamingProduct.Id, 20, 5m, 10m);
        var batchC = new InventoryBatch(StoreCId, GamingProduct.Id, 20, 5m, 10m);

        var pending = new StockReservation(batchA.Id, StoreBId, StoreAId, 2, 10m);
        var resolved = new StockReservation(batchB.Id, StoreCId, StoreBId, 1, 10m);
        resolved.Resolve(ReservationStatus.Success);

        var transferA = new StockMovement(
            StoreAId, batchA.Id, MovementType.NetworkTransferOut, 3, noon, StoreBId, 10m, 5m);
        var transferB = new StockMovement(
            StoreBId, batchB.Id, MovementType.NetworkTransferOut, 1, noon, StoreCId, 10m, 5m);

        var signals = new FakeDailyMarketSignalRepository();
        var aggregator = CreateAggregator(
            new FakeStockReservationRepository(pending, resolved),
            new FakeStockMovementRepository(transferA, transferB),
            new FakeInventoryBatchRepository(batchA, batchB, batchC),
            new FakeProductRepository(GamingProduct),
            signals);

        await aggregator.RunAsync(today, CancellationToken.None);

        var signal = signals.All.Should().ContainSingle().Subject;
        signal.ProductId.Should().Be(GamingProduct.Id);
        signal.VerticalCategory.Should().Be(VerticalCategory.Gaming);
        signal.Date.Should().Be(today);
        signal.ReservationCount.Should().Be(2);
        signal.TransferVolume.Should().Be(4);
        signal.ParticipatingStoreCount.Should().Be(3);
    }

    [Fact]
    public async Task RunAsync_ExcludesOtherVerticalsAndProductsWithoutActivity()
    {
        var today = DateTime.UtcNow.Date;
        var noon = today.AddHours(12);

        var gamingBatch = new InventoryBatch(StoreAId, GamingProduct.Id, 20, 5m, 10m);
        var pharmacyBatch = new InventoryBatch(StoreDId, PharmacyProduct.Id, 20, 1m, 2m);

        var gamingReservation = new StockReservation(gamingBatch.Id, StoreBId, StoreAId, 2, 10m);
        var pharmacyReservation = new StockReservation(pharmacyBatch.Id, StoreDId, StoreDId, 1, 2m);
        var pharmacyTransfer = new StockMovement(
            StoreDId, pharmacyBatch.Id, MovementType.NetworkTransferOut, 4, noon, StoreAId, 2m, 1m);

        var signals = new FakeDailyMarketSignalRepository();
        var aggregator = CreateAggregator(
            new FakeStockReservationRepository(gamingReservation, pharmacyReservation),
            new FakeStockMovementRepository(pharmacyTransfer),
            new FakeInventoryBatchRepository(gamingBatch, pharmacyBatch),
            new FakeProductRepository(GamingProduct, PharmacyProduct),
            signals);

        await aggregator.RunAsync(today, CancellationToken.None);

        signals.All.Should().HaveCount(2);

        var gaming = signals.All.Single(s => s.ProductId == GamingProduct.Id);
        gaming.VerticalCategory.Should().Be(VerticalCategory.Gaming);
        gaming.ReservationCount.Should().Be(1);
        gaming.TransferVolume.Should().Be(0);
        gaming.ParticipatingStoreCount.Should().Be(2);

        var pharmacy = signals.All.Single(s => s.ProductId == PharmacyProduct.Id);
        pharmacy.VerticalCategory.Should().Be(VerticalCategory.Pharmacy);
        pharmacy.ReservationCount.Should().Be(1);
        pharmacy.TransferVolume.Should().Be(4);
        pharmacy.ParticipatingStoreCount.Should().Be(2);
    }

    [Fact]
    public async Task RunAsync_Twice_UpsertsInsteadOfDuplicating()
    {
        var today = DateTime.UtcNow.Date;
        var noon = today.AddHours(12);

        var batch = new InventoryBatch(StoreAId, GamingProduct.Id, 20, 5m, 10m);
        var reservation = new StockReservation(batch.Id, StoreBId, StoreAId, 2, 10m);
        var transfer = new StockMovement(
            StoreAId, batch.Id, MovementType.NetworkTransferOut, 3, noon, StoreBId, 10m, 5m);

        var signals = new FakeDailyMarketSignalRepository();
        var aggregator = CreateAggregator(
            new FakeStockReservationRepository(reservation),
            new FakeStockMovementRepository(transfer),
            new FakeInventoryBatchRepository(batch),
            new FakeProductRepository(GamingProduct),
            signals);

        await aggregator.RunAsync(today, CancellationToken.None);
        await aggregator.RunAsync(today, CancellationToken.None);

        var signal = signals.All.Should().ContainSingle().Subject;
        signal.ReservationCount.Should().Be(1);
        signal.TransferVolume.Should().Be(3);
        signal.ParticipatingStoreCount.Should().Be(2);
    }

    [Fact]
    public async Task RunAsync_WithNoActivity_WritesNothing()
    {
        var signals = new FakeDailyMarketSignalRepository();
        var aggregator = CreateAggregator(
            new FakeStockReservationRepository(),
            new FakeStockMovementRepository(),
            new FakeInventoryBatchRepository(),
            new FakeProductRepository(GamingProduct),
            signals);

        await aggregator.RunAsync(DateTime.UtcNow.Date.AddDays(1), CancellationToken.None);

        signals.All.Should().BeEmpty();
    }

    [Fact]
    public async Task RunAsync_WithUnresolvableBatch_SkipsIt()
    {
        var orphan = new StockReservation(Guid.NewGuid(), StoreBId, StoreAId, 2, 10m);

        var signals = new FakeDailyMarketSignalRepository();
        var aggregator = CreateAggregator(
            new FakeStockReservationRepository(orphan),
            new FakeStockMovementRepository(),
            new FakeInventoryBatchRepository(),
            new FakeProductRepository(GamingProduct),
            signals);

        var act = () => aggregator.RunAsync(DateTime.UtcNow.Date, CancellationToken.None);

        await act.Should().NotThrowAsync();
        signals.All.Should().BeEmpty();
    }

    private static MarketSignalAggregator CreateAggregator(
        FakeStockReservationRepository reservationRepository,
        FakeStockMovementRepository movementRepository,
        FakeInventoryBatchRepository batchRepository,
        FakeProductRepository productRepository,
        FakeDailyMarketSignalRepository signalRepository)
    {
        return new MarketSignalAggregator(
            Clock,
            reservationRepository,
            movementRepository,
            batchRepository,
            productRepository,
            signalRepository,
            NullLogger<MarketSignalAggregator>.Instance);
    }
}