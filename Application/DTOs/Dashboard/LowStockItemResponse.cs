namespace Application.DTOs.Dashboard;

public sealed record LowStockItemResponse(
    Guid ProductId,
    string ProductName,
    int TotalQuantityRemaining,
    int ReorderPoint,
    int LeadTimeDays);