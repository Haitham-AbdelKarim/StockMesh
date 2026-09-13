using Application.Abstractions.Repositories;
using Domain.Entities;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class ProcessedStripeEventRepository : IProcessedStripeEventRepository
{
    private readonly AppDbContext _dbContext;

    public ProcessedStripeEventRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<bool> ExistsAsync(string eventId, CancellationToken cancellationToken = default)
    {
        return _dbContext.ProcessedStripeEvents
            .AnyAsync(e => e.EventId == eventId, cancellationToken);
    }

    public async Task<Guid> AddAsync(ProcessedStripeEvent stripeEvent, CancellationToken cancellationToken = default)
    {
        await _dbContext.ProcessedStripeEvents.AddAsync(stripeEvent, cancellationToken);

        return stripeEvent.Id;
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}