using Application.Abstractions.Repositories;
using Domain.Entities;
using Infrastructure.Persistence;

namespace Infrastructure.Repositories;

public class AuditLogRepository : IAuditLogRepository
{
    private readonly AppDbContext _dbContext;

    public AuditLogRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Guid> AddAsync(AuditLog entry, CancellationToken cancellationToken = default)
    {
        await _dbContext.AuditLogs.AddAsync(entry, cancellationToken);

        return entry.Id;
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}