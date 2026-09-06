namespace Application.DTOs.Inventory;

public sealed record InventoryBatchResponse(
    Guid Id,
    Guid StoreId,
    Guid ProductId,
    string ProductName,
    int QuantityRemaining,
    int SharedQuantity,
    bool IsShared,
    decimal UnitCost,
    decimal UnitSalePrice,
    int ReorderPoint,
    int LeadTimeDays,
    DateTime? ExpiryDate,
    DateTime ReceivedAt);