using Application.Abstractions.Repositories;
using Domain.Entities;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class ConversationRepository : IConversationRepository
{
    private readonly AppDbContext _dbContext;

    public ConversationRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Conversation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _dbContext.Conversations.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Conversation>> GetForUserAsync(
        Guid storeId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Conversations
            .Where(c => c.StoreId == storeId && c.UserId == userId)
            .OrderByDescending(c => c.LastActiveAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<Guid> AddAsync(Conversation conversation, CancellationToken cancellationToken = default)
    {
        await _dbContext.Conversations.AddAsync(conversation, cancellationToken);

        return conversation.Id;
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}