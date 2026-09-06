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

    public void Unshare(int quantity)
    {
        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be positive.");
        }

        if (quantity > SharedQuantity)
        {
            throw new ArgumentOutOfRangeException(
                nameof(quantity),
                $"Cannot unshare {quantity} units — only {SharedQuantity} in the shared pool.");
        }

        SharedQuantity -= quantity;
        QuantityRemaining += quantity;
        IsShared = SharedQuantity > 0;
    }

    public void UpdateDetails(
        decimal? unitSalePrice = null,
        int? reorderPoint = null,
        int? leadTimeDays = null,
        DateTime? expiryDate = null)
    {
        if (unitSalePrice is { } price && price < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(unitSalePrice), "Unit sale price cannot be negative.");
        }

        if (reorderPoint is { } reorder && reorder < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(reorderPoint), "Reorder point cannot be negative.");
        }

        if (leadTimeDays is { } lead && lead < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(leadTimeDays), "Lead time cannot be negative.");
        }

        UnitSalePrice = unitSalePrice ?? UnitSalePrice;
        ReorderPoint = reorderPoint ?? ReorderPoint;
        LeadTimeDays = leadTimeDays ?? LeadTimeDays;
        ExpiryDate = expiryDate ?? ExpiryDate;
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