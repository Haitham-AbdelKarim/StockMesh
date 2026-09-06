using Application.Common.Models;
using Application.Features.Inventory.Commands.UpdateInventoryBatch;
using Domain.Entities;
using Domain.Enums;
using FluentAssertions;
using StockMesh.Application.UnitTests.Fakes;

namespace StockMesh.Application.UnitTests.Features.Inventory;

public class UpdateInventoryBatchCommandHandlerTests
{
    private static readonly Guid StoreId = Guid.NewGuid();
    private static readonly Product Product =
        new("Paracetamol 500mg", VerticalCategory.Pharmacy, "Panadol");

    [Fact]
    public async Task Handle_WhenBatchDoesNotExist_ReturnsNotFound()
    {
        var handler = CreateHandler(new FakeInventoryBatchRepository());

        var result = await handler.Handle(
            new UpdateInventoryBatchCommand(Guid.NewGuid(), 12m),
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
            new UpdateInventoryBatchCommand(batch.Id, 12m),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Kind.Should().Be(FailureKind.NotFound);
    }

    [Fact]
    public async Task Handle_WithPartialUpdate_OnlyChangesProvidedFields()
    {
        var batch = new InventoryBatch(
            StoreId, Product.Id, 10, 5m, 10m, reorderPoint: 5, leadTimeDays: 2, expiryDate: null);
        var handler = CreateHandler(new FakeInventoryBatchRepository(batch));

        var result = await handler.Handle(
            new UpdateInventoryBatchCommand(batch.Id, 12m, 7),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        batch.UnitSalePrice.Should().Be(12m);
        batch.ReorderPoint.Should().Be(7);
        batch.LeadTimeDays.Should().Be(2);
        batch.ExpiryDate.Should().BeNull();
        result.Value!.ProductName.Should().Be(Product.Name);
    }

    [Fact]
    public async Task Handle_WithExpiryDate_SetsIt()
    {
        var batch = new InventoryBatch(StoreId, Product.Id, 10, 5m, 10m);
        var handler = CreateHandler(new FakeInventoryBatchRepository(batch));
        var expiry = DateTime.UtcNow.AddDays(30);

        var result = await handler.Handle(
            new UpdateInventoryBatchCommand(batch.Id, ExpiryDate: expiry),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        batch.ExpiryDate.Should().Be(expiry);
    }

    [Fact]
    public async Task Handle_OnSuccess_PersistsChanges()
    {
        var batch = new InventoryBatch(StoreId, Product.Id, 10, 5m, 10m);
        var repository = new FakeInventoryBatchRepository(batch);
        var handler = CreateHandler(repository);

        var result = await handler.Handle(
            new UpdateInventoryBatchCommand(batch.Id, LeadTimeDays: 4),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var persisted = await repository.GetByIdAsync(batch.Id);
        persisted!.LeadTimeDays.Should().Be(4);
    }

    private static UpdateInventoryBatchCommandHandler CreateHandler(
        FakeInventoryBatchRepository inventoryBatchRepository)
    {
        return new UpdateInventoryBatchCommandHandler(
            new FakeCurrentUser { StoreId = StoreId },
            inventoryBatchRepository,
            new FakeProductRepository(Product));
    }
}