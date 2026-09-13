using Application.Abstractions.Repositories;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class StockReservationRepository : IStockReservationRepository
{
    private readonly AppDbContext _dbContext;

    public StockReservationRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<StockReservation?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.StockReservations
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<StockReservation>> GetPendingForBatchAsync(
        Guid batchId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.StockReservations
            .Where(r => r.BatchId == batchId && r.Status == ReservationStatus.Pending)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<StockReservation>> GetByStatusAsync(
        ReservationStatus status,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.StockReservations
            .Where(r => r.Status == status)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<StockReservation>> GetExpiredPendingAsync(
        DateTime nowUtc,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.StockReservations
            .Where(r => r.Status == ReservationStatus.Pending && r.HoldExpiresAt <= nowUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<StockReservation>> GetForStoreByDateRangeAsync(
        Guid storeId,
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.StockReservations
            .Where(r => (r.RequestingStoreId == storeId || r.OwningStoreId == storeId)
                && r.CreatedAt >= from
                && r.CreatedAt < to)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<StockReservation>> GetByDateRangeAsync(
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.StockReservations
            .Where(r => r.CreatedAt >= from && r.CreatedAt < to)
            .ToListAsync(cancellationToken);
    }

    public async Task<(IReadOnlyList<StockReservation> Items, int TotalCount)> GetForStoreAsync(
        Guid storeId,
        bool? incoming,
        ReservationStatus? status,
        DateTime? from,
        DateTime? to,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.StockReservations.AsQueryable();

        if (incoming is true)
        {
            query = query.Where(r => r.OwningStoreId == storeId);
        }
        else if (incoming is false)
        {
            query = query.Where(r => r.RequestingStoreId == storeId);
        }
        else
        {
            query = query.Where(r => r.RequestingStoreId == storeId || r.OwningStoreId == storeId);
        }

        if (status is { } reservationStatus)
        {
            query = query.Where(r => r.Status == reservationStatus);
        }

        if (from is { } fromDate)
        {
            query = query.Where(r => r.CreatedAt >= fromDate);
        }

        if (to is { } toDate)
        {
            query = query.Where(r => r.CreatedAt <= toDate);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<Guid> AddAsync(
        StockReservation reservation,
        CancellationToken cancellationToken = default)
    {
        await _dbContext.StockReservations.AddAsync(reservation, cancellationToken);
        return reservation.Id;
    }

    public void Update(StockReservation reservation)
    {
        _dbContext.StockReservations.Update(reservation);
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.SaveChangesAsync(cancellationToken);
    }
}