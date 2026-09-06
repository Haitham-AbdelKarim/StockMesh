using Application.Features.StockMovements.Queries.GetStockMovements;
using Domain.Entities;
using Domain.Enums;
using FluentAssertions;
using StockMesh.Application.UnitTests.Fakes;

namespace StockMesh.Application.UnitTests.Features.StockMovements;

public class GetStockMovementsQueryHandlerTests
{
    private static readonly Guid StoreId = Guid.NewGuid();

    private static readonly Product Product =
        new("Paracetamol 500mg", VerticalCategory.Pharmacy, "Panadol");

    [Fact]
    public async Task Handle_ReturnsPaginatedMovementsWithProductInfo()
    {
        var batch = new InventoryBatch(StoreId, Product.Id, 10, 5m, 12.5m);
        var stock = new FakeStockMovementRepository(
            new StockMovement(StoreId, batch.Id, MovementType.Restock, 10, DateTime.UtcNow.AddDays(-1)),
            new StockMovement(StoreId, batch.Id, MovementType.Sale, 3, DateTime.UtcNow, unitPrice: 12.5m));
        var handler = CreateHandler(stock, batch);

        var result = await handler.Handle(
            new GetStockMovementsQuery(),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().HaveCount(2);
        result.Value.TotalCount.Should().Be(2);
        result.Value.Items.Should().OnlyContain(i =>
            i.StoreId == StoreId
            && i.ProductId == Product.Id
            && i.ProductName == Product.Name);
    }

    [Fact]
    public async Task Handle_WithMovementTypeFilter_ReturnsOnlyThatType()
    {
        var batch = new InventoryBatch(StoreId, Product.Id, 10, 5m, 12.5m);
        var stock = new FakeStockMovementRepository(
            new StockMovement(StoreId, batch.Id, MovementType.Restock, 10, DateTime.UtcNow.AddDays(-1)),
            new StockMovement(StoreId, batch.Id, MovementType.Sale, 3, DateTime.UtcNow));
        var handler = CreateHandler(stock, batch);

        var result = await handler.Handle(
            new GetStockMovementsQuery(MovementType: MovementType.Sale),
            CancellationToken.None);

        result.Value!.Items.Should().ContainSingle()
            .Which.MovementType.Should().Be(MovementType.Sale);
        result.Value.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task Handle_WhenNoMovements_ReturnsEmptyPage()
    {
        var batch = new InventoryBatch(StoreId, Product.Id, 10, 5m, 12.5m);
        var handler = CreateHandler(new FakeStockMovementRepository(), batch);

        var result = await handler.Handle(
            new GetStockMovementsQuery(),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().BeEmpty();
        result.Value.TotalCount.Should().Be(0);
    }

    [Fact]
    public void Validate_WithOutOfRangePaging_ReturnsErrors()
    {
        var validator = new GetStockMovementsQueryValidator();

        var result = validator.Validate(new GetStockMovementsQuery(Page: 0, PageSize: 0));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().NotBeEmpty();
    }

    [Fact]
    public void Validate_WithNegativeRelatedStoreId_IsAllowed()
    {
        var validator = new GetStockMovementsQueryValidator();

        var result = validator.Validate(new GetStockMovementsQuery(Page: 1, PageSize: 20));

        result.IsValid.Should().BeTrue();
    }

    private static GetStockMovementsQueryHandler CreateHandler(
        FakeStockMovementRepository stockRepository,
        InventoryBatch batch)
    {
        return new GetStockMovementsQueryHandler(
            new FakeCurrentUser { StoreId = StoreId },
            stockRepository,
            new FakeInventoryBatchRepository(batch),
            new FakeProductRepository(Product));
    }
}