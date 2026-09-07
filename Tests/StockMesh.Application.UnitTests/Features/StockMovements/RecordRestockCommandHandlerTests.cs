using Application.Common.Models;
using Application.DTOs.StockMovements;
using Application.Features.StockMovements.Commands.RecordRestock;
using Domain.Entities;
using Domain.Enums;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using StockMesh.Application.UnitTests.Fakes;

namespace StockMesh.Application.UnitTests.Features.StockMovements;

public class RecordRestockCommandHandlerTests
{
    private static readonly Guid StoreId = Guid.NewGuid();
    private static readonly FakeDateTimeProvider Clock = new();

    private static readonly Product Product =
        new("Paracetamol 500mg", VerticalCategory.Pharmacy, "Panadol");

    [Fact]
    public async Task Handle_WhenProductDoesNotExist_ReturnsBadRequest()
    {
        var handler = CreateHandler(
            new FakeStockMovementRepository(),
            new FakeInventoryBatchRepository(),
            new FakeProductRepository());

        var result = await handler.Handle(
            new RecordRestockCommand(Guid.NewGuid(), 10, 5m),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Kind.Should().Be(FailureKind.BadRequest);
    }

    [Fact]
    public async Task Handle_WhenProductVerticalMismatch_ReturnsBadRequest()
    {
        var gamingProduct = new Product("PlayStation 5 Console", VerticalCategory.Gaming, "Sony");
        var handler = CreateHandler(
            new FakeStockMovementRepository(),
            new FakeInventoryBatchRepository(),
            new FakeProductRepository(gamingProduct));

        var result = await handler.Handle(
            new RecordRestockCommand(gamingProduct.Id, 10, 5m),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Kind.Should().Be(FailureKind.BadRequest);
    }

    [Fact]
    public async Task Handle_WithNoPreviousBatch_DefaultsUnitSalePriceToUnitCost()
    {
        var batchRepository = new FakeInventoryBatchRepository();
        var movementRepository = new FakeStockMovementRepository();
        var handler = CreateHandler(
            movementRepository,
            batchRepository,
            new FakeProductRepository(Product));

        var result = await handler.Handle(
            new RecordRestockCommand(Product.Id, 12, 4.5m, SupplierName: "Supplier X"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.BatchId.Should().NotBe(Guid.Empty);
        result.Value.Should().BeEquivalentTo(new RestockResponse(
            result.Value.BatchId, Product.Id, 12, 4.5m, 4.5m));

        batchRepository.Count.Should().Be(1);
        movementRepository.All.Should().ContainSingle().Which.Should().Match<StockMovement>(m =>
            m.StoreId == StoreId
            && m.BatchId == result.Value.BatchId
            && m.MovementType == MovementType.Restock
            && m.Quantity == 12
            && m.UnitCost == 4.5m
            && m.UnitPrice == null
            && m.SupplierName == "Supplier X");
    }

    [Fact]
    public async Task Handle_WithPreviousBatch_DefaultsToLatestUnitSalePrice()
    {
        var batchRepository = new FakeInventoryBatchRepository(
            new InventoryBatch(StoreId, Product.Id, 5, 5m, 22.9m, receivedAt: Clock.UtcNow.AddDays(-1)));
        var handler = CreateHandler(
            new FakeStockMovementRepository(),
            batchRepository,
            new FakeProductRepository(Product));

        var result = await handler.Handle(
            new RecordRestockCommand(Product.Id, 8, 5.5m),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.UnitSalePrice.Should().Be(22.9m);
    }

    private static RecordRestockCommandHandler CreateHandler(
        FakeStockMovementRepository movementRepository,
        FakeInventoryBatchRepository batchRepository,
        FakeProductRepository productRepository)
    {
        return new RecordRestockCommandHandler(
            new FakeCurrentUser
            {
                StoreId = StoreId,
                VerticalCategory = VerticalCategory.Pharmacy
            },
            Clock,
            batchRepository,
            productRepository,
            movementRepository,
            new FakeDailyMetricsMaterializer(),
            new FakeUnitOfWork(),
            NullLogger<RecordRestockCommandHandler>.Instance);
    }
}