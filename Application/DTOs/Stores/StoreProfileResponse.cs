using Domain.Enums;

namespace Application.DTOs.Stores;

public sealed record StoreProfileResponse(
    Guid StoreId,
    string Name,
    VerticalCategory VerticalCategory,
    double Latitude,
    double Longitude,
    double MaxSearchRadiusKm,
    bool IsVerified);