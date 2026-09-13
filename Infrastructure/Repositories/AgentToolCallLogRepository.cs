using Application.Abstractions.Repositories;
using Domain.Entities;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class AgentToolCallLogRepository : IAgentToolCallLogRepository
{
    private readonly AppDbContext _dbContext;

    public AgentToolCallLogRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<AgentToolCallLog>> GetRecentTurnsAsync(
        Guid conversationId,
        int count,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.AgentToolCallLogs
            .Where(l => l.ConversationId == conversationId)
            .OrderByDescending(l => l.CreatedAt)
            .Take(count)
            .OrderBy(l => l.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<Guid> AddAsync(AgentToolCallLog log, CancellationToken cancellationToken = default)
    {
        await _dbContext.AgentToolCallLogs.AddAsync(log, cancellationToken);

        return log.Id;
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}