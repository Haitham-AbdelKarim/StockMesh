using Domain.Entities;

namespace Application.Abstractions.Repositories;

public interface IReservationPaymentRepository
{
    Task<ReservationPayment?> GetByReservationIdAsync(
        Guid reservationId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<Guid, ReservationPayment>> GetByReservationIdsAsync(
        IReadOnlyCollection<Guid> reservationIds,
        CancellationToken cancellationToken = default);

    Task<Guid> AddAsync(ReservationPayment payment, CancellationToken cancellationToken = default);

    void Update(ReservationPayment payment);

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}