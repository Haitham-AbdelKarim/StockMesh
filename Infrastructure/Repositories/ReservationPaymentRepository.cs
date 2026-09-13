using Application.Abstractions.Repositories;
using Domain.Entities;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class ReservationPaymentRepository : IReservationPaymentRepository
{
    private readonly AppDbContext _dbContext;

    public ReservationPaymentRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<ReservationPayment?> GetByReservationIdAsync(
        Guid reservationId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.ReservationPayments
            .SingleOrDefaultAsync(p => p.ReservationId == reservationId, cancellationToken);
    }

    public async Task<IReadOnlyDictionary<Guid, ReservationPayment>> GetByReservationIdsAsync(
        IReadOnlyCollection<Guid> reservationIds,
        CancellationToken cancellationToken = default)
    {
        if (reservationIds.Count == 0)
        {
            return new Dictionary<Guid, ReservationPayment>();
        }

        var payments = await _dbContext.ReservationPayments
            .Where(p => reservationIds.Contains(p.ReservationId))
            .ToListAsync(cancellationToken);

        return payments.ToDictionary(p => p.ReservationId);
    }

    public async Task<Guid> AddAsync(ReservationPayment payment, CancellationToken cancellationToken = default)
    {
        await _dbContext.ReservationPayments.AddAsync(payment, cancellationToken);

        return payment.Id;
    }

    public void Update(ReservationPayment payment)
    {
        _dbContext.ReservationPayments.Update(payment);
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}