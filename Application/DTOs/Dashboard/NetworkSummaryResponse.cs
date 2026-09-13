namespace Application.DTOs.Dashboard;

public sealed record NetworkSummaryResponse(
    int TransfersOutCount,
    int TransfersInCount,
    int TransfersOutUnits,
    int TransfersInUnits,
    decimal TransfersOutRevenue,
    decimal TransfersInValue,
    decimal ReservationSuccessRate);