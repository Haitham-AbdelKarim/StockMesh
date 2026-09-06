namespace Application.DTOs.StockMovements;

public sealed record SaleResponse(
    Guid MovementId,
    Guid BatchId,
    int Quantity,
    decimal UnitSalePrice,
    decimal TotalPrice);