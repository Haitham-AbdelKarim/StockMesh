namespace Application.DTOs.Dashboard;

public sealed record SalesTrendPointResponse(
    DateTime Date,
    int UnitsSold,
    decimal SalesRevenue);