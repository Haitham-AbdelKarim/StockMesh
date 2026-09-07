using Domain.Entities;
using Domain.Enums;

namespace Application.Abstractions.Repositories;

public interface IStockReservationRepository
{
    Task<StockReservation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<StockReservation>> GetPendingForBatchAsync(
        Guid batchId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<StockReservation>> GetByStatusAsync(
        ReservationStatus status,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<StockReservation>> GetExpiredPendingAsync(
        DateTime nowUtc,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<StockReservation>> GetForStoreByDateRangeAsync(
        Guid storeId,
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<StockReservation> Items, int TotalCount)> GetForStoreAsync(
        Guid storeId,
        bool? incoming,
        ReservationStatus? status,
        DateTime? from,
        DateTime? to,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<Guid> AddAsync(StockReservation reservation, CancellationToken cancellationToken = default);

    void Update(StockReservation reservation);

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}