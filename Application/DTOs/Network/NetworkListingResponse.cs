namespace Application.DTOs.Network;

public sealed record NetworkListingResponse(
    Guid BatchId,
    Guid StoreId,
    Guid ProductId,
    string ProductName,
    string StoreName,
    double DistanceKm,
    int SharedQuantity,
    decimal UnitSalePrice,
    DateTime? ExpiryDate);