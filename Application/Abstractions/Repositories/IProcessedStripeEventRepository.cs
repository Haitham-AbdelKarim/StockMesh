using Domain.Entities;

namespace Application.Abstractions.Repositories;

public interface IProcessedStripeEventRepository
{
    Task<bool> ExistsAsync(string eventId, CancellationToken cancellationToken = default);

    Task<Guid> AddAsync(ProcessedStripeEvent stripeEvent, CancellationToken cancellationToken = default);

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}