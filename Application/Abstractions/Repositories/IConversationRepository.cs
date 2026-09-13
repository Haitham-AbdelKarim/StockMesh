using Domain.Entities;

namespace Application.Abstractions.Repositories;

public interface IConversationRepository
{
    Task<Conversation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Conversation>> GetForUserAsync(
        Guid storeId,
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<Guid> AddAsync(Conversation conversation, CancellationToken cancellationToken = default);

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}