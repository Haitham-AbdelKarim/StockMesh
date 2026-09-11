namespace Application.DTOs.Recommendations;

public sealed record MarketSignalPointResponse(
    DateTime Date,
    int ReservationCount,
    int TransferVolume,
    int ParticipatingStoreCount);