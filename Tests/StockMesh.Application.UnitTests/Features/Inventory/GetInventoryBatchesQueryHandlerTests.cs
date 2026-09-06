using Application.Features.Inventory.Queries.GetInventoryBatches;
using Domain.Entities;
using Domain.Enums;
using FluentAssertions;
using StockMesh.Application.UnitTests.Fakes;

namespace StockMesh.Application.UnitTests.Features.Inventory;

public class GetInventoryBatchesQueryHandlerTests
{
    private static readonly Guid StoreId = Guid.NewGuid();
    private static readonly Guid OtherStoreId = Guid.NewGuid();
    private static readonly Product ProductA =
        new("Paracetamol 500mg", VerticalCategory.Pharmacy, "Panadol");
    private static readonly Product ProductB =
        new("Vitamin C 1000mg", VerticalCategory.Pharmacy);

    [Fact]
    public async Task Handle_ReturnsOnlyOwnStoresBatchesOrderedByReceivedAtDesc()
    {
        var newer = new InventoryBatch(StoreId, ProductA.Id, 20, 5m, 12m, receivedAt: DateTime.UtcNow);
        var older = new InventoryBatch(StoreId, ProductA.Id, 10, 5m, 12m, receivedAt: DateTime.UtcNow.AddDays(-2));
        var foreign = new InventoryBatch(OtherStoreId, ProductA.Id, 99, 1m, 2m, receivedAt: DateTime.UtcNow);
        var handler = CreateHandler(new FakeInventoryBatchRepository(newer, older, foreign));

        var result = await handler.Handle(
            new GetInventoryBatchesQuery(null, 1, 20),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().BeInDescendingOrder(b => b.ReceivedAt);
        result.Value.TotalCount.Should().Be(2);
        result.Value.Items.Should().OnlyContain(b => b.StoreId == StoreId);
        result.Value.Items[0].ProductName.Should().Be(ProductA.Name);
    }

    [Fact]
    public async Task Handle_WithProductFilter_ReturnsOnlyThatProduct()
    {
        var batchA = new InventoryBatch(StoreId, ProductA.Id, 10, 5m, 12m, receivedAt: DateTime.UtcNow);
        var batchB = new InventoryBatch(StoreId, ProductB.Id, 8, 3m, 6m, receivedAt: DateTime.UtcNow);
        var handler = CreateHandler(new FakeInventoryBatchRepository(batchA, batchB));

        var result = await handler.Handle(
            new GetInventoryBatchesQuery(ProductB.Id, 1, 20),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().ContainSingle();
        result.Value.Items[0].ProductId.Should().Be(ProductB.Id);
        result.Value.Items[0].ProductName.Should().Be(ProductB.Name);
    }

    [Fact]
    public async Task Handle_WithPaging_ReturnsRequestedSlice()
    {
        var batch1 = new InventoryBatch(StoreId, ProductA.Id, 1, 5m, 12m, receivedAt: DateTime.UtcNow.AddDays(-1));
        var batch2 = new InventoryBatch(StoreId, ProductA.Id, 2, 5m, 12m, receivedAt: DateTime.UtcNow.AddDays(-2));
        var batch3 = new InventoryBatch(StoreId, ProductB.Id, 3, 3m, 6m, receivedAt: DateTime.UtcNow.AddDays(-3));
        var handler = CreateHandler(new FakeInventoryBatchRepository(batch1, batch2, batch3));

        var result = await handler.Handle(
            new GetInventoryBatchesQuery(null, 2, 2),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.TotalCount.Should().Be(3);
        result.Value.Items.Should().HaveCount(1);
        result.Value.Page.Should().Be(2);
        result.Value.TotalPages.Should().Be(2);
    }

    private static GetInventoryBatchesQueryHandler CreateHandler(
        FakeInventoryBatchRepository batchRepository)
    {
        return new GetInventoryBatchesQueryHandler(
            new FakeCurrentUser { StoreId = StoreId },
            batchRepository,
            new FakeProductRepository(ProductA, ProductB));
    }
}