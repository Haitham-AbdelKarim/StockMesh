namespace Application.DTOs.Dashboard;

public sealed record TopSellerResponse(
    Guid ProductId,
    string ProductName,
    int UnitsSold,
    decimal SalesRevenue);