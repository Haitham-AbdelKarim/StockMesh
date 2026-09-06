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

    [Fact]
    public void Unshare_moves_shared_quantity_back_to_remaining()
    {
        var batch = CreateBatch(quantityRemaining: 10, sharedQuantity: 3);

        batch.Unshare(2);

        batch.QuantityRemaining.Should().Be(9);
        batch.SharedQuantity.Should().Be(1);
        batch.IsShared.Should().BeTrue();
    }

    [Fact]
    public void Unshare_all_marks_batch_as_not_shared()
    {
        var batch = CreateBatch(quantityRemaining: 10, sharedQuantity: 3);

        batch.Unshare(3);

        batch.QuantityRemaining.Should().Be(10);
        batch.SharedQuantity.Should().Be(0);
        batch.IsShared.Should().BeFalse();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Unshare_throws_when_quantity_is_zero_or_negative(int quantity)
    {
        var batch = CreateBatch(quantityRemaining: 10, sharedQuantity: 3);

        var act = () => batch.Unshare(quantity);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Unshare_throws_when_exceeding_shared_quantity()
    {
        var batch = CreateBatch(quantityRemaining: 10, sharedQuantity: 2);

        var act = () => batch.Unshare(5);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void UpdateDetails_only_changes_provided_fields()
    {
        var batch = CreateBatch(quantityRemaining: 10, sharedQuantity: 3);

        batch.UpdateDetails(unitSalePrice: 12m, reorderPoint: 7);

        batch.UnitSalePrice.Should().Be(12m);
        batch.ReorderPoint.Should().Be(7);
        batch.LeadTimeDays.Should().Be(0);
        batch.ExpiryDate.Should().BeNull();
        batch.QuantityRemaining.Should().Be(7);
        batch.SharedQuantity.Should().Be(3);
    }

    [Fact]
    public void UpdateDetails_throws_when_unit_sale_price_is_negative()
    {
        var batch = CreateBatch(quantityRemaining: 10);

        var act = () => batch.UpdateDetails(unitSalePrice: -1m);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void UpdateDetails_throws_when_reorder_point_is_negative()
    {
        var batch = CreateBatch(quantityRemaining: 10);

        var act = () => batch.UpdateDetails(reorderPoint: -1);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void UpdateDetails_throws_when_lead_time_is_negative()
    {
        var batch = CreateBatch(quantityRemaining: 10);

        var act = () => batch.UpdateDetails(leadTimeDays: -1);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void UpdateDetails_sets_expiry_date()
    {
        var batch = CreateBatch(quantityRemaining: 10);
        var expiry = DateTime.UtcNow.AddDays(30);

        batch.UpdateDetails(expiryDate: expiry);

        batch.ExpiryDate.Should().Be(expiry);
    }
}