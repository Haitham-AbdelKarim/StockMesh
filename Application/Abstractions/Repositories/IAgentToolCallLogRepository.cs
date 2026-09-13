using Domain.Entities;

namespace Application.Abstractions.Repositories;

public interface IAgentToolCallLogRepository
{
    Task<IReadOnlyList<AgentToolCallLog>> GetRecentTurnsAsync(
        Guid conversationId,
        int count,
        CancellationToken cancellationToken = default);

    Task<Guid> AddAsync(AgentToolCallLog log, CancellationToken cancellationToken = default);

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}