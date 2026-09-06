using Application.Common.Models;
using Application.Features.Inventory.Queries.GetBatchById;
using Domain.Entities;
using Domain.Enums;
using FluentAssertions;
using StockMesh.Application.UnitTests.Fakes;

namespace StockMesh.Application.UnitTests.Features.Inventory;

public class GetBatchByIdQueryHandlerTests
{
    private static readonly Guid StoreId = Guid.NewGuid();
    private static readonly Product Product =
        new("Paracetamol 500mg", VerticalCategory.Pharmacy, "Panadol");

    [Fact]
    public async Task Handle_WhenBatchDoesNotExist_ReturnsNotFound()
    {
        var handler = CreateHandler(new FakeInventoryBatchRepository());

        var result = await handler.Handle(
            new GetBatchByIdQuery(Guid.NewGuid()),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Kind.Should().Be(FailureKind.NotFound);
    }

    [Fact]
    public async Task Handle_WhenBatchBelongsToAnotherStore_ReturnsNotFound()
    {
        var batch = new InventoryBatch(Guid.NewGuid(), Product.Id, 10, 5m, 10m);
        var handler = CreateHandler(new FakeInventoryBatchRepository(batch));

        var result = await handler.Handle(
            new GetBatchByIdQuery(batch.Id),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Kind.Should().Be(FailureKind.NotFound);
    }

    [Fact]
    public async Task Handle_WithOwnBatch_ReturnsResponseWithProductName()
    {
        var batch = new InventoryBatch(StoreId, Product.Id, 10, 5m, 12m, reorderPoint: 3);
        batch.MarkAsShared(2);
        var handler = CreateHandler(new FakeInventoryBatchRepository(batch));

        var result = await handler.Handle(
            new GetBatchByIdQuery(batch.Id),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Id.Should().Be(batch.Id);
        result.Value.ProductName.Should().Be(Product.Name);
        result.Value.QuantityRemaining.Should().Be(8);
        result.Value.SharedQuantity.Should().Be(2);
        result.Value.IsShared.Should().BeTrue();
        result.Value.ReorderPoint.Should().Be(3);
    }

    private static GetBatchByIdQueryHandler CreateHandler(
        FakeInventoryBatchRepository inventoryBatchRepository)
    {
        return new GetBatchByIdQueryHandler(
            new FakeCurrentUser { StoreId = StoreId },
            inventoryBatchRepository,
            new FakeProductRepository(Product));
    }
}