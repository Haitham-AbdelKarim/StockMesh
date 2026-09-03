using Domain.Common;
using Domain.Exceptions;

namespace Domain.Entities;

public class InventoryBatch : BaseEntity
{
    public Guid StoreId { get; private set; }

    public Guid ProductId { get; private set; }

    public int QuantityRemaining { get; private set; }

    public int SharedQuantity { get; private set; }

    public bool IsShared { get; private set; }

    public decimal UnitCost { get; private set; }

    public decimal UnitSalePrice { get; private set; }

    public int ReorderPoint { get; private set; }

    public int LeadTimeDays { get; private set; }

    public DateTime? ExpiryDate { get; private set; }

    public DateTime ReceivedAt { get; private set; }

    public byte[] RowVersion { get; private set; } = [];

    private InventoryBatch()
    {
    }

    public InventoryBatch(
        Guid storeId,
        Guid productId,
        int quantityRemaining,
        decimal unitCost,
        decimal unitSalePrice,
        int reorderPoint = 0,
        int leadTimeDays = 0,
        DateTime? expiryDate = null,
        DateTime? receivedAt = null)
    {
        StoreId = storeId;
        ProductId = productId;
        QuantityRemaining = quantityRemaining;
        UnitCost = unitCost;
        UnitSalePrice = unitSalePrice;
        ReorderPoint = reorderPoint;
        LeadTimeDays = leadTimeDays;
        ExpiryDate = expiryDate;
        ReceivedAt = receivedAt ?? DateTime.UtcNow;
    }

    public void MarkAsShared(int quantity)
    {
        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be positive.");
        }

        if (quantity > QuantityRemaining)
        {
            throw new ArgumentOutOfRangeException(
                nameof(quantity),
                $"Cannot share {quantity} units — only {QuantityRemaining} remaining.");
        }

        QuantityRemaining -= quantity;
        SharedQuantity += quantity;
        IsShared = SharedQuantity > 0;
    }

    public void ReserveFromSharedPool(int quantity)
    {
        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be positive.");
        }

        if (quantity > SharedQuantity)
        {
            throw new InsufficientStockException(
                $"Cannot reserve {quantity} units — only {SharedQuantity} available in shared pool.");
        }

        SharedQuantity -= quantity;

        if (SharedQuantity == 0)
        {
            IsShared = false;
        }
    }

    public void ReleaseToSharedPool(int quantity)
    {
        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be positive.");
        }

        SharedQuantity += quantity;
        IsShared = true;
    }

    public void ConsumePrivate(int quantity)
    {
        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be positive.");
        }

        if (quantity > QuantityRemaining)
        {
            throw new InsufficientStockException(
                $"Cannot consume {quantity} units — only {QuantityRemaining} remaining in private stock.");
        }

        QuantityRemaining -= quantity;
    }
}