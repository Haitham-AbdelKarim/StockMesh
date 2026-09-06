using Domain.Entities;

namespace Application.Abstractions.Repositories;

public interface IAuditLogRepository
{
    Task<Guid> AddAsync(AuditLog entry, CancellationToken cancellationToken = default);

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}