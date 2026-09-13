using Application.Abstractions.Repositories;
using Application.Abstractions.Services;
using Application.Common.Models;
using Application.DTOs.Assistant;
using MediatR;

namespace Application.Features.Assistant.Queries.GetConversationById;

public sealed class GetConversationByIdQueryHandler :
    IRequestHandler<GetConversationByIdQuery, Result<ConversationDetailResponse>>
{
    private readonly ICurrentUser _currentUser;
    private readonly IConversationRepository _conversationRepository;
    private readonly IAgentToolCallLogRepository _logRepository;

    public GetConversationByIdQueryHandler(
        ICurrentUser currentUser,
        IConversationRepository conversationRepository,
        IAgentToolCallLogRepository logRepository)
    {
        _currentUser = currentUser;
        _conversationRepository = conversationRepository;
        _logRepository = logRepository;
    }

    public async Task<Result<ConversationDetailResponse>> Handle(
        GetConversationByIdQuery query,
        CancellationToken cancellationToken)
    {
        var conversation = await _conversationRepository.GetByIdAsync(
            query.ConversationId,
            cancellationToken);

        if (conversation is null
            || conversation.StoreId != _currentUser.StoreId
            || conversation.UserId != _currentUser.UserId)
        {
            return Result<ConversationDetailResponse>.NotFound("Conversation not found.");
        }

        var turns = await _logRepository.GetRecentTurnsAsync(
            conversation.Id,
            100,
            cancellationToken);

        return Result<ConversationDetailResponse>.Success(new ConversationDetailResponse(
            conversation.Id,
            conversation.Title,
            turns.Select(t => new ConversationTurnResponse(
                t.Question,
                t.ToolCalls,
                t.FinalAnswer,
                t.CreatedAt)).ToList()));
    }
}