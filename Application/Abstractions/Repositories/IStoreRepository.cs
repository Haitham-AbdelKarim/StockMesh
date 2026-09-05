using Domain.Entities;

namespace Application.Abstractions.Repositories;

public interface IStoreRepository
{
    Task<Store?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Guid> AddAsync(Store store, CancellationToken cancellationToken = default);

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}