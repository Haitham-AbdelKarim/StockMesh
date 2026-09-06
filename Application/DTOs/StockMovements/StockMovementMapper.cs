using Domain.Entities;

namespace Application.DTOs.StockMovements;

internal static class StockMovementMapper
{
    public static StockMovementResponse ToResponse(
        StockMovement movement,
        Guid productId,
        string productName)
    {
        return new StockMovementResponse(
            movement.Id,
            movement.StoreId,
            movement.BatchId,
            movement.RelatedStoreId,
            movement.MovementType,
            movement.Quantity,
            movement.UnitPrice,
            movement.UnitCost,
            movement.SupplierName,
            movement.Note,
            movement.OccurredAt,
            productId,
            productName);
    }
}