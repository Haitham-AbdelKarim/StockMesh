using Application.Features.Inventory.Queries.GetProductStock;
using Domain.Entities;
using Domain.Enums;
using FluentAssertions;
using StockMesh.Application.UnitTests.Fakes;

namespace StockMesh.Application.UnitTests.Features.Inventory;

public class GetProductStockQueryHandlerTests
{
    private static readonly Guid StoreId = Guid.NewGuid();
    private static readonly Product ProductA =
        new("Paracetamol 500mg", VerticalCategory.Pharmacy, "Panadol");
    private static readonly Product ProductB =
        new("Vitamin C 1000mg", VerticalCategory.Pharmacy);

    [Fact]
    public async Task Handle_AggregatesTotalsPerProduct()
    {
        var batch1 = new InventoryBatch(StoreId, ProductA.Id, 10, 5m, 12m, receivedAt: DateTime.UtcNow);
        batch1.MarkAsShared(2);
        var batch2 = new InventoryBatch(StoreId, ProductA.Id, 20, 5m, 12m, receivedAt: DateTime.UtcNow.AddDays(-1));
        var batch3 = new InventoryBatch(StoreId, ProductB.Id, 8, 3m, 6m, receivedAt: DateTime.UtcNow);
        var handler = CreateHandler(new FakeInventoryBatchRepository(batch1, batch2, batch3));

        var result = await handler.Handle(
            new GetProductStockQuery(1, 20),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var productA = result.Value!.Items.Single(x => x.ProductId == ProductA.Id);
        productA.ProductName.Should().Be(ProductA.Name);
        productA.TotalQuantityRemaining.Should().Be(28);
        productA.TotalSharedQuantity.Should().Be(2);
        productA.BatchCount.Should().Be(2);

        var productB = result.Value.Items.Single(x => x.ProductId == ProductB.Id);
        productB.TotalQuantityRemaining.Should().Be(8);
        productB.BatchCount.Should().Be(1);
    }

    [Fact]
    public async Task Handle_ExcludesOtherStoresBatches()
    {
        var own = new InventoryBatch(StoreId, ProductA.Id, 10, 5m, 12m, receivedAt: DateTime.UtcNow);
        var foreign = new InventoryBatch(Guid.NewGuid(), ProductA.Id, 99, 1m, 2m, receivedAt: DateTime.UtcNow);
        var handler = CreateHandler(new FakeInventoryBatchRepository(own, foreign));

        var result = await handler.Handle(
            new GetProductStockQuery(1, 20),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var productA = result.Value!.Items.Single(x => x.ProductId == ProductA.Id);
        productA.TotalQuantityRemaining.Should().Be(10);
    }

    [Fact]
    public async Task Handle_WithPaging_ReturnsRequestedSlice()
    {
        var batch1 = new InventoryBatch(StoreId, ProductA.Id, 1, 5m, 12m, receivedAt: DateTime.UtcNow);
        var batch2 = new InventoryBatch(StoreId, ProductA.Id, 2, 5m, 12m, receivedAt: DateTime.UtcNow);
        var batch3 = new InventoryBatch(StoreId, ProductB.Id, 3, 3m, 6m, receivedAt: DateTime.UtcNow);
        var handler = CreateHandler(new FakeInventoryBatchRepository(batch1, batch2, batch3));

        var result = await handler.Handle(
            new GetProductStockQuery(2, 1),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.TotalCount.Should().Be(2);
        result.Value.Items.Should().ContainSingle();
        result.Value.Page.Should().Be(2);
        result.Value.TotalPages.Should().Be(2);
    }

    private static GetProductStockQueryHandler CreateHandler(
        FakeInventoryBatchRepository inventoryBatchRepository)
    {
        return new GetProductStockQueryHandler(
            new FakeCurrentUser { StoreId = StoreId },
            inventoryBatchRepository,
            new FakeProductRepository(ProductA, ProductB));
    }
}