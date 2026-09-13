using Application.Abstractions.Repositories;
using Application.Abstractions.Services;
using Application.Common.Models;
using Application.DTOs.Assistant;
using Application.Features.Assistant.Services;
using Domain.Entities;
using MediatR;

namespace Application.Features.Assistant.Commands.AskAssistant;

public sealed class AskAssistantCommandHandler :
    IRequestHandler<AskAssistantCommand, Result<AssistantStreamTicket>>
{
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly IConversationRepository _conversationRepository;
    private readonly IAgentToolCallLogRepository _logRepository;
    private readonly IAssistantRateLimiter _rateLimiter;

    public AskAssistantCommandHandler(
        ICurrentUser currentUser,
        IDateTimeProvider clock,
        IConversationRepository conversationRepository,
        IAgentToolCallLogRepository logRepository,
        IAssistantRateLimiter rateLimiter)
    {
        _currentUser = currentUser;
        _clock = clock;
        _conversationRepository = conversationRepository;
        _logRepository = logRepository;
        _rateLimiter = rateLimiter;
    }

    public async Task<Result<AssistantStreamTicket>> Handle(
        AskAssistantCommand command,
        CancellationToken cancellationToken)
    {
        if (!_rateLimiter.TryAcquire(_currentUser.UserId, out var retryAfterSeconds))
        {
            return Result<AssistantStreamTicket>.RateLimited(
                $"Question quota exceeded. Try again in {retryAfterSeconds} seconds.");
        }

        Conversation conversation;

        if (command.ConversationId.HasValue)
        {
            var existing = await _conversationRepository.GetByIdAsync(
                command.ConversationId.Value,
                cancellationToken);

            if (existing is null
                || existing.StoreId != _currentUser.StoreId
                || existing.UserId != _currentUser.UserId)
            {
                return Result<AssistantStreamTicket>.NotFound("Conversation not found.");
            }

            existing.Touch(_clock.UtcNow);
            await _conversationRepository.SaveChangesAsync(cancellationToken);

            conversation = existing;
        }
        else
        {
            conversation = new Conversation(
                _currentUser.StoreId,
                _currentUser.UserId,
                command.Question,
                _clock.UtcNow);

            await _conversationRepository.AddAsync(conversation, cancellationToken);
            await _conversationRepository.SaveChangesAsync(cancellationToken);
        }

        var turns = await _logRepository.GetRecentTurnsAsync(
            conversation.Id,
            HistoryTrimmer.MaxTurns,
            cancellationToken);

        var history = HistoryTrimmer.Trim(turns.Select(t => (t.Question, t.FinalAnswer)));

        return Result<AssistantStreamTicket>.Success(
            new AssistantStreamTicket(
                conversation.Id,
                _currentUser.StoreId,
                _currentUser.UserId,
                command.Question,
                history));
    }
}