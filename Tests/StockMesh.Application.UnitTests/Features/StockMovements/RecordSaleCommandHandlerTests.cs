using Application.Common.Models;
using Application.DTOs.StockMovements;
using Application.Features.StockMovements.Commands.RecordSale;
using Domain.Entities;
using Domain.Enums;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using StockMesh.Application.UnitTests.Fakes;

namespace StockMesh.Application.UnitTests.Features.StockMovements;

public class RecordSaleCommandHandlerTests
{
    private static readonly Guid StoreId = Guid.NewGuid();
    private static readonly Guid OtherStoreId = Guid.NewGuid();
    private static readonly FakeDateTimeProvider Clock = new();

    private static readonly InventoryBatch Batch =
        new(StoreId, Guid.NewGuid(), 10, 5m, 12.5m);

    [Fact]
    public async Task Handle_WhenBatchDoesNotExist_ReturnsNotFoundAndRollsBack()
    {
        var unitOfWork = new FakeUnitOfWork();
        var handler = CreateHandler(new FakeStockMovementRepository(), unitOfWork);

        var result = await handler.Handle(
            new RecordSaleCommand(Guid.NewGuid(), 2),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Kind.Should().Be(FailureKind.NotFound);
        unitOfWork.Transactions.Should().ContainSingle().Subject.RolledBack.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WhenBatchBelongsToAnotherStore_ReturnsForbidden()
    {
        var otherBatch = new InventoryBatch(OtherStoreId, Guid.NewGuid(), 5, 4m, 9m);
        var handler = CreateHandler(
            new FakeStockMovementRepository(),
            batches: otherBatch);

        var result = await handler.Handle(
            new RecordSaleCommand(otherBatch.Id, 1),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Kind.Should().Be(FailureKind.Forbidden);
        otherBatch.QuantityRemaining.Should().Be(5);
    }

    [Fact]
    public async Task Handle_WhenInsufficientPrivateStock_ReturnsConflictAndRollsBack()
    {
        var unitOfWork = new FakeUnitOfWork();
        var handler = CreateHandler(new FakeStockMovementRepository(), unitOfWork);

        var result = await handler.Handle(
            new RecordSaleCommand(Batch.Id, 11),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Kind.Should().Be(FailureKind.Conflict);
        Batch.QuantityRemaining.Should().Be(10);
        unitOfWork.Transactions.Should().ContainSingle().Subject.RolledBack.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_OnSuccess_ConsumesPrivateStockAndRecordsSaleMovement()
    {
        var batch = new InventoryBatch(StoreId, Guid.NewGuid(), 10, 5m, 12.5m);
        var movements = new FakeStockMovementRepository();
        var unitOfWork = new FakeUnitOfWork();
        var handler = CreateHandler(movements, unitOfWork, batch);

        var result = await handler.Handle(
            new RecordSaleCommand(batch.Id, 3),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.MovementId.Should().NotBe(Guid.Empty);
        result.Value.Should().BeEquivalentTo(new SaleResponse(
            result.Value.MovementId, batch.Id, 3, 12.5m, 37.5m));

        batch.QuantityRemaining.Should().Be(7);

        movements.All.Should().ContainSingle().Which.Should().Match<StockMovement>(m =>
            m.StoreId == StoreId
            && m.BatchId == batch.Id
            && m.MovementType == MovementType.Sale
            && m.Quantity == 3
            && m.UnitPrice == 12.5m
            && m.UnitCost == 5m
            && m.RelatedStoreId == null);

        unitOfWork.Transactions.Should().ContainSingle().Subject.Committed.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_OnConcurrentModification_RollsBackAndReturnsConflict()
    {
        var movements = new FakeStockMovementRepository
        {
            SaveException = new DbUpdateConcurrencyException("Concurrent update.")
        };
        var unitOfWork = new FakeUnitOfWork();
        var handler = CreateHandler(movements, unitOfWork, Batch);

        var result = await handler.Handle(
            new RecordSaleCommand(Batch.Id, 2),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Kind.Should().Be(FailureKind.Conflict);
        unitOfWork.Transactions.Should().ContainSingle().Subject.RolledBack.Should().BeTrue();
    }

    private static RecordSaleCommandHandler CreateHandler(
        FakeStockMovementRepository movementRepository,
        FakeUnitOfWork? unitOfWork = null,
        params InventoryBatch[] batches)
    {
        var allBatches = batches.Length == 0 ? new[] { Batch } : batches;

        return new RecordSaleCommandHandler(
            new FakeCurrentUser { StoreId = StoreId, UserId = Guid.NewGuid() },
            Clock,
            new FakeInventoryBatchRepository(allBatches),
            movementRepository,
            new FakeDailyMetricsMaterializer(),
            unitOfWork ?? new FakeUnitOfWork(),
            NullLogger<RecordSaleCommandHandler>.Instance);
    }
}