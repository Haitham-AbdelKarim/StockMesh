using Application.Features.Metrics.Services;
using Domain.Entities;
using Domain.Enums;
using FluentAssertions;
using StockMesh.Application.UnitTests.Fakes;

namespace StockMesh.Application.UnitTests.Features.Metrics;

public class DailyMetricsMaterializerTests
{
    private const int Day = 1;
    private static readonly DateTime DayStart = new(2026, 6, Day, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime Now = new(2026, 6, Day, 12, 0, 0, DateTimeKind.Utc);
    private static readonly Guid StoreId = Guid.NewGuid();
    private static readonly Guid Product1Id = Guid.NewGuid();
    private static readonly Guid Product2Id = Guid.NewGuid();

    private static InventoryBatch NewBatch(Guid productId, int remaining = 100)
    {
        return new InventoryBatch(StoreId, productId, remaining, 5m, 12.5m);
    }

    [Fact]
    public async Task RecomputeDayAsync_ComputesStoreAndProductRollups()
    {
        var batch1 = NewBatch(Product1Id);
        var batch2 = NewBatch(Product2Id);
        var batch3 = NewBatch(Product1Id);

        var movements = new FakeStockMovementRepository(
            new StockMovement(StoreId, batch1.Id, MovementType.Restock, 100, Now, unitCost: 5m),
            new StockMovement(StoreId, batch1.Id, MovementType.Sale, 20, Now, unitPrice: 12.5m, unitCost: 5m),
            new StockMovement(StoreId, batch1.Id, MovementType.NetworkTransferOut, 10, Now, relatedStoreId: Guid.NewGuid(), unitPrice: 12.5m, unitCost: 5m),
            new StockMovement(StoreId, batch3.Id, MovementType.NetworkTransferIn, 8, Now, relatedStoreId: Guid.NewGuid(), unitPrice: 4m),
            new StockMovement(StoreId, batch2.Id, MovementType.Sale, 5, Now, unitPrice: 9m, unitCost: 3m));

        var expenses = new FakeExpenseRepository(
            new Expense(StoreId, "Rent", 20m, Now));

        var storeMetrics = new FakeDailyStoreMetricRepository();
        var productMetrics = new FakeDailyProductMetricRepository();

        var materializer = CreateMaterializer(
            movements,
            expenses,
            storeMetrics,
            productMetrics,
            batch1, batch2, batch3);

        await materializer.RecomputeDayAsync([StoreId], DayStart, CancellationToken.None);

        var storeMetric = await storeMetrics.GetAsync(StoreId, DayStart, CancellationToken.None);

        storeMetric.Should().NotBeNull();
        storeMetric!.SalesRevenue.Should().Be(295m);
        storeMetric.TransfersOutRevenue.Should().Be(125m);
        storeMetric.CostOfGoodsSold.Should().Be(165m);
        storeMetric.StockPurchases.Should().Be(532m);
        storeMetric.ExpenseTotal.Should().Be(20m);
        storeMetric.NetProfit.Should().Be(295m + 125m - 165m - 532m - 20m);
        storeMetric.UnitsSold.Should().Be(25);
        storeMetric.TransfersOutUnits.Should().Be(10);
        storeMetric.TransfersInUnits.Should().Be(8);

        var product1 = await productMetrics.GetAsync(StoreId, Product1Id, DayStart, CancellationToken.None);

        product1.Should().NotBeNull();
        product1!.UnitsSold.Should().Be(20);
        product1.SalesRevenue.Should().Be(250m);
        product1.SalesCost.Should().Be(100m);
        product1.TransfersOutUnits.Should().Be(10);
        product1.TransfersOutRevenue.Should().Be(125m);

        var product2 = await productMetrics.GetAsync(StoreId, Product2Id, DayStart, CancellationToken.None);

        product2.Should().NotBeNull();
        product2!.UnitsSold.Should().Be(5);
        product2.TransfersOutUnits.Should().Be(0);
    }

    [Fact]
    public async Task RecomputeDayAsync_IsIdempotent()
    {
        var batch = NewBatch(Product1Id);

        var movements = new FakeStockMovementRepository(
            new StockMovement(StoreId, batch.Id, MovementType.Sale, 4, Now, unitPrice: 10m, unitCost: 5m));

        var storeMetrics = new FakeDailyStoreMetricRepository();
        var productMetrics = new FakeDailyProductMetricRepository();

        var materializer = CreateMaterializer(
            movements,
            new FakeExpenseRepository(),
            storeMetrics,
            productMetrics,
            batch);

        await materializer.RecomputeDayAsync([StoreId], DayStart, CancellationToken.None);
        await materializer.RecomputeDayAsync([StoreId], DayStart, CancellationToken.None);

        storeMetrics.All.Should().ContainSingle();
        productMetrics.All.Should().ContainSingle();

        var storeMetric = await storeMetrics.GetAsync(StoreId, DayStart, CancellationToken.None);
        var productMetric = await productMetrics.GetAsync(StoreId, Product1Id, DayStart, CancellationToken.None);

        storeMetric!.UnitsSold.Should().Be(4);
        storeMetric.SalesRevenue.Should().Be(40m);
        productMetric!.UnitsSold.Should().Be(4);
        productMetric.SalesRevenue.Should().Be(40m);
    }

    [Fact]
    public async Task RecomputeDayAsync_IgnoresMovementsFromStoresNotRequested()
    {
        var otherStoreId = Guid.NewGuid();
        var otherBatch = new InventoryBatch(otherStoreId, Product1Id, 10, 5m, 12.5m);
        var batch = NewBatch(Product1Id);

        var movements = new FakeStockMovementRepository(
            new StockMovement(otherStoreId, otherBatch.Id, MovementType.Sale, 50, Now, unitPrice: 10m, unitCost: 5m),
            new StockMovement(StoreId, batch.Id, MovementType.Sale, 2, Now, unitPrice: 10m, unitCost: 5m));

        var storeMetrics = new FakeDailyStoreMetricRepository();

        var materializer = CreateMaterializer(
            movements,
            new FakeExpenseRepository(),
            storeMetrics,
            new FakeDailyProductMetricRepository(),
            batch, otherBatch);

        await materializer.RecomputeDayAsync([StoreId], DayStart, CancellationToken.None);

        var storeMetric = await storeMetrics.GetAsync(StoreId, DayStart, CancellationToken.None);

        storeMetric!.UnitsSold.Should().Be(2);
    }

    [Fact]
    public async Task RecomputeDayAsync_MovementsOutsideDay_AreExcluded()
    {
        var batch = NewBatch(Product1Id);

        var movements = new FakeStockMovementRepository(
            new StockMovement(StoreId, batch.Id, MovementType.Sale, 3, DayStart.AddDays(-1), unitPrice: 10m, unitCost: 5m),
            new StockMovement(StoreId, batch.Id, MovementType.Sale, 3, DayStart.AddDays(1), unitPrice: 10m, unitCost: 5m));

        var storeMetrics = new FakeDailyStoreMetricRepository();

        var materializer = CreateMaterializer(
            movements,
            new FakeExpenseRepository(),
            storeMetrics,
            new FakeDailyProductMetricRepository(),
            batch);

        await materializer.RecomputeDayAsync([StoreId], DayStart, CancellationToken.None);

        (await storeMetrics.GetAsync(StoreId, DayStart, CancellationToken.None)).Should().BeNull();
    }

    [Fact]
    public async Task RecomputeDayAsync_StoreWithExpensesOnly_GetsRollup()
    {
        var expenses = new FakeExpenseRepository(
            new Expense(StoreId, "Utilities", 35m, Now));

        var storeMetrics = new FakeDailyStoreMetricRepository();

        var materializer = CreateMaterializer(
            new FakeStockMovementRepository(),
            expenses,
            storeMetrics,
            new FakeDailyProductMetricRepository());

        await materializer.RecomputeDayAsync([StoreId], DayStart, CancellationToken.None);

        var storeMetric = await storeMetrics.GetAsync(StoreId, DayStart, CancellationToken.None);

        storeMetric.Should().NotBeNull();
        storeMetric!.ExpenseTotal.Should().Be(35m);
        storeMetric.NetProfit.Should().Be(-35m);
        storeMetric.UnitsSold.Should().Be(0);
    }

    private static DailyMetricsMaterializer CreateMaterializer(
        FakeStockMovementRepository movements,
        FakeExpenseRepository expenses,
        FakeDailyStoreMetricRepository storeMetrics,
        FakeDailyProductMetricRepository productMetrics,
        params InventoryBatch[] batches)
    {
        return new DailyMetricsMaterializer(
            movements,
            expenses,
            new FakeInventoryBatchRepository(batches),
            storeMetrics,
            productMetrics);
    }
}