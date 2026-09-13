using Application.Common.Models;
using Application.Features.Inventory.Commands.ToggleBatchSharing;
using Domain.Entities;
using Domain.Enums;
using FluentAssertions;
using StockMesh.Application.UnitTests.Fakes;

namespace StockMesh.Application.UnitTests.Features.Inventory;

public class ToggleBatchSharingCommandHandlerTests
{
    private static readonly Guid StoreId = Guid.NewGuid();
    private static readonly Product Product =
        new("Paracetamol 500mg", VerticalCategory.Pharmacy, "Panadol");

    [Fact]
    public async Task Handle_WhenBatchDoesNotExist_ReturnsNotFound()
    {
        var handler = CreateHandler(new FakeInventoryBatchRepository());

        var result = await handler.Handle(
            new ToggleBatchSharingCommand(Guid.NewGuid(), true, 2),
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
            new ToggleBatchSharingCommand(batch.Id, true, 2),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Kind.Should().Be(FailureKind.NotFound);
    }

    [Fact]
    public async Task Handle_ShareExceedingRemaining_ReturnsBadRequest()
    {
        var batch = new InventoryBatch(StoreId, Product.Id, 2, 5m, 10m);
        var auditLog = new FakeAuditLogRepository();
        var handler = CreateHandler(new FakeInventoryBatchRepository(batch), auditLog);

        var result = await handler.Handle(
            new ToggleBatchSharingCommand(batch.Id, true, 5),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Kind.Should().Be(FailureKind.BadRequest);
        batch.QuantityRemaining.Should().Be(2);
        batch.SharedQuantity.Should().Be(0);
        auditLog.Entries.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ValidShare_MovesQuantityIntoSharedPoolAndWritesAudit()
    {
        var batch = new InventoryBatch(StoreId, Product.Id, 10, 5m, 10m);
        var auditLog = new FakeAuditLogRepository();
        var handler = CreateHandler(new FakeInventoryBatchRepository(batch), auditLog);

        var result = await handler.Handle(
            new ToggleBatchSharingCommand(batch.Id, true, 3),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.QuantityRemaining.Should().Be(7);
        result.Value.SharedQuantity.Should().Be(3);
        result.Value.IsShared.Should().BeTrue();

        auditLog.Entries.Should().ContainSingle().Which.Should().Match<AuditLog>(a =>
            a.EntityType == nameof(InventoryBatch)
            && a.EntityId == batch.Id
            && a.Action == "batch.shared"
            && a.ActorStoreId == StoreId);
    }

    [Fact]
    public async Task Handle_UnshareSpecificAmount_ReturnsQuantityRemainingAndStaysSharedIfResidual()
    {
        var batch = new InventoryBatch(StoreId, Product.Id, 10, 5m, 10m);
        batch.MarkAsShared(3);
        var auditLog = new FakeAuditLogRepository();
        var handler = CreateHandler(new FakeInventoryBatchRepository(batch), auditLog);

        var result = await handler.Handle(
            new ToggleBatchSharingCommand(batch.Id, false, 2),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.QuantityRemaining.Should().Be(9);
        result.Value.SharedQuantity.Should().Be(1);
        result.Value.IsShared.Should().BeTrue();

        auditLog.Entries.Should().ContainSingle().Which.Should().Match<AuditLog>(a =>
            a.EntityType == nameof(InventoryBatch)
            && a.EntityId == batch.Id
            && a.Action == "batch.unshared"
            && a.ActorStoreId == StoreId);
    }

    [Fact]
    public async Task Handle_UnshareExceedingSharedQuantity_ReturnsBadRequest()
    {
        var batch = new InventoryBatch(StoreId, Product.Id, 10, 5m, 10m);
        batch.MarkAsShared(2);
        var auditLog = new FakeAuditLogRepository();
        var handler = CreateHandler(new FakeInventoryBatchRepository(batch), auditLog);

        var result = await handler.Handle(
            new ToggleBatchSharingCommand(batch.Id, false, 5),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Kind.Should().Be(FailureKind.BadRequest);
        batch.QuantityRemaining.Should().Be(8);
        batch.SharedQuantity.Should().Be(2);
        auditLog.Entries.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_UnshareAllWhenSharedQuantityZero_ReturnsSuccessUnchanged()
    {
        var batch = new InventoryBatch(StoreId, Product.Id, 10, 5m, 10m);
        var auditLog = new FakeAuditLogRepository();
        var handler = CreateHandler(new FakeInventoryBatchRepository(batch), auditLog);

        var result = await handler.Handle(
            new ToggleBatchSharingCommand(batch.Id, false, 0),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.QuantityRemaining.Should().Be(10);
        result.Value.SharedQuantity.Should().Be(0);
        result.Value.IsShared.Should().BeFalse();
        auditLog.Entries.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_UnshareAll_MarksBatchAsNotShared()
    {
        var batch = new InventoryBatch(StoreId, Product.Id, 10, 5m, 10m);
        batch.MarkAsShared(3);
        var repository = new FakeInventoryBatchRepository(batch);
        var handler = CreateHandler(repository);

        var result = await handler.Handle(
            new ToggleBatchSharingCommand(batch.Id, false, 0),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.QuantityRemaining.Should().Be(10);
        result.Value.SharedQuantity.Should().Be(0);
        result.Value.IsShared.Should().BeFalse();

        var persisted = await repository.GetByIdAsync(batch.Id);
        persisted!.SharedQuantity.Should().Be(0);
        persisted.IsShared.Should().BeFalse();
    }

    private static ToggleBatchSharingCommandHandler CreateHandler(
        FakeInventoryBatchRepository inventoryBatchRepository,
        FakeAuditLogRepository? auditLogRepository = null)
    {
        return new ToggleBatchSharingCommandHandler(
            new FakeCurrentUser { StoreId = StoreId },
            inventoryBatchRepository,
            new FakeProductRepository(Product),
            auditLogRepository ?? new FakeAuditLogRepository());
    }
}