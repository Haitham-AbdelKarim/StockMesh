namespace Application.DTOs.Inventory;

public sealed record ProductStockSummaryResponse(
    Guid ProductId,
    string ProductName,
    int TotalQuantityRemaining,
    int TotalSharedQuantity,
    int BatchCount);