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