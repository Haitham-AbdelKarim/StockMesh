using Application.Common.Models;
using Application.Features.Inventory.Commands.AddInventoryBatch;
using Domain.Entities;
using Domain.Enums;
using FluentAssertions;
using StockMesh.Application.UnitTests.Fakes;

namespace StockMesh.Application.UnitTests.Features.Inventory;

public class AddInventoryBatchCommandHandlerTests
{
    private static readonly Guid StoreId = Guid.NewGuid();
    private static readonly Product Product =
        new("Paracetamol 500mg", VerticalCategory.Pharmacy, "Panadol");

    [Fact]
    public async Task Handle_WhenProductDoesNotExist_ReturnsBadRequest()
    {
        var handler = CreateHandler(
            new FakeInventoryBatchRepository(),
            new FakeProductRepository());

        var result = await handler.Handle(
            new AddInventoryBatchCommand(Guid.NewGuid(), 10, 5m),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Kind.Should().Be(FailureKind.BadRequest);
    }

    [Fact]
    public async Task Handle_WhenProductVerticalMismatch_ReturnsBadRequest()
    {
        var handler = CreateHandler(
            new FakeInventoryBatchRepository(),
            new FakeProductRepository(new Product("PlayStation 5 Console", VerticalCategory.Gaming, "Sony")));

        var result = await handler.Handle(
            new AddInventoryBatchCommand(Product.Id, 10, 5m),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Kind.Should().Be(FailureKind.BadRequest);
    }

    [Fact]
    public async Task Handle_WithNoPreviousBatch_DefaultsUnitSalePriceToUnitCost()
    {
        var handler = CreateHandler(
            new FakeInventoryBatchRepository(),
            new FakeProductRepository(Product));

        var result = await handler.Handle(
            new AddInventoryBatchCommand(Product.Id, 10, 5m),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.UnitSalePrice.Should().Be(5m);
        result.Value.QuantityRemaining.Should().Be(10);
        result.Value.ProductName.Should().Be(Product.Name);
        result.Value.IsShared.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_WithPreviousBatch_DefaultsToLatestUnitSalePrice()
    {
        var repository = new FakeInventoryBatchRepository(
            new InventoryBatch(StoreId, Product.Id, 5, 5m, 24.5m, receivedAt: DateTime.UtcNow.AddDays(-10)),
            new InventoryBatch(StoreId, Product.Id, 4, 5m, 22.9m, receivedAt: DateTime.UtcNow.AddDays(-1)));
        var handler = CreateHandler(repository, new FakeProductRepository(Product));

        var result = await handler.Handle(
            new AddInventoryBatchCommand(Product.Id, 8, 5.5m),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.UnitSalePrice.Should().Be(22.9m);
    }

    [Fact]
    public async Task Handle_WithExplicitUnitSalePrice_UsesIt()
    {
        var handler = CreateHandler(
            new FakeInventoryBatchRepository(),
            new FakeProductRepository(Product));

        var result = await handler.Handle(
            new AddInventoryBatchCommand(Product.Id, 8, 5m, 30m),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.UnitSalePrice.Should().Be(30m);
    }

    [Fact]
    public async Task Handle_OnSuccess_AddsNewBatchRow()
    {
        var repository = new FakeInventoryBatchRepository();
        var handler = CreateHandler(repository, new FakeProductRepository(Product));

        var result = await handler.Handle(
            new AddInventoryBatchCommand(Product.Id, 3, 4m),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Id.Should().NotBe(Guid.Empty);
        repository.Count.Should().Be(1);
    }

    private static AddInventoryBatchCommandHandler CreateHandler(
        FakeInventoryBatchRepository inventoryBatchRepository,
        FakeProductRepository productRepository)
    {
        return new AddInventoryBatchCommandHandler(
            new FakeCurrentUser { StoreId = StoreId, VerticalCategory = VerticalCategory.Pharmacy },
            inventoryBatchRepository,
            productRepository);
    }
}