namespace Application.DTOs.Dashboard;

public sealed record SlowMoverResponse(
    Guid ProductId,
    string ProductName,
    int UnitsSoldLast7Days,
    int UnitsSoldLastDays,
    int QuantityRemaining,
    int EstimatedDaysOfCover);