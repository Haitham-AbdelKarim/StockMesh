using Application.Abstractions.Repositories;
using Application.Abstractions.Services;
using Domain.Entities;

namespace StockMesh.Application.UnitTests.Fakes;

internal sealed class FakeConversationRepository : IConversationRepository
{
    private readonly List<Conversation> _conversations;

    public FakeConversationRepository(params Conversation[] conversations)
    {
        _conversations = conversations.ToList();
    }

    public Task<Conversation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_conversations.FirstOrDefault(c => c.Id == id));
    }

    public Task<IReadOnlyList<Conversation>> GetForUserAsync(
        Guid storeId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<Conversation>>(
            _conversations
                .Where(c => c.StoreId == storeId && c.UserId == userId)
                .OrderByDescending(c => c.LastActiveAt)
                .ToList());
    }

    public Task<Guid> AddAsync(Conversation conversation, CancellationToken cancellationToken = default)
    {
        _conversations.Add(conversation);

        return Task.FromResult(conversation.Id);
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(0);
    }
}

internal sealed class FakeAgentToolCallLogRepository : IAgentToolCallLogRepository
{
    private readonly List<AgentToolCallLog> _logs;

    public FakeAgentToolCallLogRepository(params AgentToolCallLog[] logs)
    {
        _logs = logs.ToList();
    }

    public IReadOnlyList<AgentToolCallLog> All => _logs;

    public Task<IReadOnlyList<AgentToolCallLog>> GetRecentTurnsAsync(
        Guid conversationId,
        int count,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<AgentToolCallLog>>(
            _logs.Where(l => l.ConversationId == conversationId)
                .OrderByDescending(l => l.CreatedAt)
                .Take(count)
                .OrderBy(l => l.CreatedAt)
                .ToList());
    }

    public Task<Guid> AddAsync(AgentToolCallLog log, CancellationToken cancellationToken = default)
    {
        _logs.Add(log);

        return Task.FromResult(log.Id);
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(0);
    }
}

internal sealed class FakeAssistantRateLimiter : IAssistantRateLimiter
{
    public bool Allow { get; set; } = true;

    public int RetryAfterSeconds { get; set; } = 30;

    public int Calls { get; private set; }

    public bool TryAcquire(Guid userId, out int retryAfterSeconds)
    {
        Calls++;
        retryAfterSeconds = Allow ? 0 : RetryAfterSeconds;

        return Allow;
    }
}