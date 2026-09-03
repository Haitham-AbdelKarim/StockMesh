using Domain.Entities;
using Domain.Exceptions;
using FluentAssertions;

namespace StockMesh.Domain.UnitTests.Entities;

public class InventoryBatchTests
{
    private static InventoryBatch CreateBatch(
        int quantityRemaining = 10,
        int sharedQuantity = 0,
        decimal unitCost = 5m,
        decimal unitSalePrice = 10m)
    {
        var batch = new InventoryBatch(
            Guid.NewGuid(),
            Guid.NewGuid(),
            quantityRemaining,
            unitCost,
            unitSalePrice);

        if (sharedQuantity > 0)
        {
            batch.MarkAsShared(sharedQuantity);
        }

        return batch;
    }

    [Fact]
    public void MarkAsShared_moves_quantity_correctly_between_fields()
    {
        var batch = CreateBatch(quantityRemaining: 10);

        batch.MarkAsShared(3);

        batch.QuantityRemaining.Should().Be(7);
        batch.SharedQuantity.Should().Be(3);
    }

    [Fact]
    public void MarkAsShared_throws_when_exceeding_remaining()
    {
        var batch = CreateBatch(quantityRemaining: 2);

        var act = () => batch.MarkAsShared(5);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void MarkAsShared_throws_when_quantity_is_zero_or_negative()
    {
        var batch = CreateBatch(quantityRemaining: 10);

        var actZero = () => batch.MarkAsShared(0);
        var actNegative = () => batch.MarkAsShared(-1);

        actZero.Should().Throw<ArgumentOutOfRangeException>();
        actNegative.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void ReserveFromSharedPool_decrements_shared_quantity()
    {
        var batch = CreateBatch(quantityRemaining: 10, sharedQuantity: 5);

        batch.ReserveFromSharedPool(3);

        batch.SharedQuantity.Should().Be(2);
    }

    [Fact]
    public void ReserveFromSharedPool_throws_when_insufficient()
    {
        var batch = CreateBatch(quantityRemaining: 10, sharedQuantity: 2);

        var act = () => batch.ReserveFromSharedPool(5);

        act.Should().Throw<InsufficientStockException>();
    }

    [Fact]
    public void ReleaseToSharedPool_increments_shared_quantity()
    {
        var batch = CreateBatch(quantityRemaining: 10, sharedQuantity: 3);

        batch.ReleaseToSharedPool(2);

        batch.SharedQuantity.Should().Be(5);
        batch.IsShared.Should().BeTrue();
    }

    [Fact]
    public void ConsumePrivate_decrements_quantity_remaining()
    {
        var batch = CreateBatch(quantityRemaining: 10);

        batch.ConsumePrivate(4);

        batch.QuantityRemaining.Should().Be(6);
    }

    [Fact]
    public void ConsumePrivate_throws_when_insufficient()
    {
        var batch = CreateBatch(quantityRemaining: 3);

        var act = () => batch.ConsumePrivate(8);

        act.Should().Throw<InsufficientStockException>();
    }

    [Fact]
    public void ConsumePrivate_never_affects_shared_quantity()
    {
        var batch = CreateBatch(quantityRemaining: 10, sharedQuantity: 4);

        batch.ConsumePrivate(3);

        batch.SharedQuantity.Should().Be(4);
        batch.QuantityRemaining.Should().Be(3);
    }

    [Fact]
    public void Two_batches_same_store_and_product_coexist()
    {
        var storeId = Guid.NewGuid();
        var productId = Guid.NewGuid();

        var batch1 = new InventoryBatch(storeId, productId, 10, 5m, 10m);
        var batch2 = new InventoryBatch(storeId, productId, 20, 7m, 14m);

        batch1.Id.Should().NotBe(batch2.Id);
        batch1.StoreId.Should().Be(storeId);
        batch2.StoreId.Should().Be(storeId);
        batch1.ProductId.Should().Be(productId);
        batch2.ProductId.Should().Be(productId);
    }
}