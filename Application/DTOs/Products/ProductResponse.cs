using Domain.Enums;

namespace Application.DTOs.Products;

public sealed record ProductResponse(
    Guid Id,
    string Name,
    VerticalCategory VerticalCategory,
    string? Brand,
    string? Barcode);