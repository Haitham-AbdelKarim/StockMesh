namespace Application.DTOs.Dashboard;

public sealed record DashboardSummaryResponse(
    DateTime Date,
    decimal SalesRevenue,
    decimal TransfersOutRevenue,
    decimal CostOfGoodsSold,
    decimal StockPurchases,
    decimal ExpenseTotal,
    decimal NetProfit,
    int UnitsSold,
    int TransfersOutUnits,
    int TransfersInUnits);