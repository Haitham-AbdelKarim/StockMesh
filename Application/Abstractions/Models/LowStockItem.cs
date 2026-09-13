namespace Application.Abstractions.Models;

public sealed record LowStockItem(
    Guid ProductId,
    int TotalQuantityRemaining,
    int ReorderPoint,
    int LeadTimeDays);