namespace Application.DTOs.StockMovements;

public sealed record RestockResponse(
    Guid BatchId,
    Guid ProductId,
    int Quantity,
    decimal UnitCost,
    decimal UnitSalePrice);