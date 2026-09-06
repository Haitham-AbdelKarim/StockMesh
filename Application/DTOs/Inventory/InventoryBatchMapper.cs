using Domain.Entities;

namespace Application.DTOs.Inventory;

internal static class InventoryBatchMapper
{
    public static InventoryBatchResponse ToResponse(InventoryBatch batch, string productName)
    {
        return new InventoryBatchResponse(
            batch.Id,
            batch.StoreId,
            batch.ProductId,
            productName,
            batch.QuantityRemaining,
            batch.SharedQuantity,
            batch.IsShared,
            batch.UnitCost,
            batch.UnitSalePrice,
            batch.ReorderPoint,
            batch.LeadTimeDays,
            batch.ExpiryDate,
            batch.ReceivedAt);
    }
}