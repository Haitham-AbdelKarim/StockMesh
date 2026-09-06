namespace Application.Abstractions.Models;

public sealed record ProductStockSummary(
    Guid ProductId,
    int TotalQuantityRemaining,
    int TotalSharedQuantity,
    int BatchCount);