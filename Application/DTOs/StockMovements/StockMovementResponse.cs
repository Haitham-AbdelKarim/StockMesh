using Domain.Enums;

namespace Application.DTOs.StockMovements;

public sealed record StockMovementResponse(
    Guid Id,
    Guid StoreId,
    Guid BatchId,
    Guid? RelatedStoreId,
    MovementType MovementType,
    int Quantity,
    decimal? UnitPrice,
    decimal? UnitCost,
    string? SupplierName,
    string? Note,
    DateTime OccurredAt,
    Guid ProductId,
    string ProductName);