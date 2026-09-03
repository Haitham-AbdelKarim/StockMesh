using Domain.Common;
using Domain.Enums;

namespace Domain.Entities;

public class StockMovement : BaseEntity
{
    public Guid StoreId { get; init; }

    public Guid BatchId { get; init; }

    public Guid? RelatedStoreId { get; init; }

    public MovementType MovementType { get; init; }

    public int Quantity { get; init; }

    public decimal? UnitPrice { get; init; }

    public decimal? UnitCost { get; init; }

    public string? SupplierName { get; init; }

    public string? Note { get; init; }

    public DateTime OccurredAt { get; init; }

    public Guid? StaffUserId { get; init; }

    public StockMovement(
        Guid storeId,
        Guid batchId,
        MovementType movementType,
        int quantity,
        DateTime occurredAt,
        Guid? relatedStoreId = null,
        decimal? unitPrice = null,
        decimal? unitCost = null,
        string? supplierName = null,
        string? note = null,
        Guid? staffUserId = null)
    {
        StoreId = storeId;
        BatchId = batchId;
        MovementType = movementType;
        Quantity = quantity;
        OccurredAt = occurredAt;
        RelatedStoreId = relatedStoreId;
        UnitPrice = unitPrice;
        UnitCost = unitCost;
        SupplierName = supplierName;
        Note = note;
        StaffUserId = staffUserId;
    }
}