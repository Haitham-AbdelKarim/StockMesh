using Application.Features.Dashboard.Queries.GetLowStock;
using Domain.Entities;
using Domain.Enums;
using FluentAssertions;
using StockMesh.Application.UnitTests.Fakes;

namespace StockMesh.Application.UnitTests.Features.Dashboard;

public class GetLowStockQueryHandlerTests
{
    private static readonly Guid StoreId = Guid.NewGuid();
    private static readonly Product Product = new("Paracetamol", VerticalCategory.Pharmacy, "Panadol");

    [Fact]
    public async Task Handle_ReturnsProductsAtOrBelowReorderPoint()
    {
        var lowStockBatch = new InventoryBatch(StoreId, Product.Id, 5, 5m, 12.5m, reorderPoint: 10, leadTimeDays: 3);
        var okBatch = new InventoryBatch(
            StoreId,
            Guid.NewGuid(),
            50,
            5m,
            12.5m,
            reorderPoint: 10,
            leadTimeDays: 3);

        var handler = new GetLowStockQueryHandler(
            new FakeCurrentUser { StoreId = StoreId },
            new FakeInventoryBatchRepository(lowStockBatch, okBatch),
            new FakeProductRepository(Product));

        var result = await handler.Handle(new GetLowStockQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle();
        result.Value.Single().ProductId.Should().Be(Product.Id);
        result.Value.Single().ProductName.Should().Be(Product.Name);
        result.Value.Single().TotalQuantityRemaining.Should().Be(5);
        result.Value.Single().ReorderPoint.Should().Be(10);
        result.Value.Single().LeadTimeDays.Should().Be(3);
    }

    [Fact]
    public async Task Handle_WhenNothingLowOnStock_ReturnsEmpty()
    {
        var batch = new InventoryBatch(StoreId, Product.Id, 50, 5m, 12.5m, reorderPoint: 10);

        var handler = new GetLowStockQueryHandler(
            new FakeCurrentUser { StoreId = StoreId },
            new FakeInventoryBatchRepository(batch),
            new FakeProductRepository(Product));

        var result = await handler.Handle(new GetLowStockQuery(), CancellationToken.None);

        result.Value.Should().BeEmpty();
    }
}