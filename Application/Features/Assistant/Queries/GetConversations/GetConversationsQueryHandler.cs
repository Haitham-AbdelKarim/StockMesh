using Application.Abstractions.Repositories;
using Application.Abstractions.Services;
using Application.Common.Models;
using Application.DTOs.Assistant;
using MediatR;

namespace Application.Features.Assistant.Queries.GetConversations;

public sealed class GetConversationsQueryHandler :
    IRequestHandler<GetConversationsQuery, Result<IReadOnlyList<ConversationResponse>>>
{
    private readonly ICurrentUser _currentUser;
    private readonly IConversationRepository _conversationRepository;

    public GetConversationsQueryHandler(
        ICurrentUser currentUser,
        IConversationRepository conversationRepository)
    {
        _currentUser = currentUser;
        _conversationRepository = conversationRepository;
    }

    public async Task<Result<IReadOnlyList<ConversationResponse>>> Handle(
        GetConversationsQuery query,
        CancellationToken cancellationToken)
    {
        var conversations = await _conversationRepository.GetForUserAsync(
            _currentUser.StoreId,
            _currentUser.UserId,
            cancellationToken);

        IReadOnlyList<ConversationResponse> response = conversations
            .Select(c => new ConversationResponse(c.Id, c.Title, c.LastActiveAt))
            .ToList();

        return Result<IReadOnlyList<ConversationResponse>>.Success(response);
    }
}