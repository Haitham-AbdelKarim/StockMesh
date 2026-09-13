using Application.Abstractions.Repositories;
using Application.Common.Models;
using Application.DTOs.AgentLogs;
using Domain.Entities;
using MediatR;

namespace Application.Features.AgentLogs.Commands.RecordAgentLog;

public sealed class RecordAgentLogCommandHandler :
    IRequestHandler<RecordAgentLogCommand, Result<AgentLogResponse>>
{
    private readonly IAgentToolCallLogRepository _logRepository;

    public RecordAgentLogCommandHandler(IAgentToolCallLogRepository logRepository)
    {
        _logRepository = logRepository;
    }

    public async Task<Result<AgentLogResponse>> Handle(
        RecordAgentLogCommand command,
        CancellationToken cancellationToken)
    {
        var log = new AgentToolCallLog(
            command.StoreId,
            command.UserId,
            command.ConversationId,
            command.Question,
            command.ToolCalls,
            command.FinalAnswer);

        var id = await _logRepository.AddAsync(log, cancellationToken);
        await _logRepository.SaveChangesAsync(cancellationToken);

        return Result<AgentLogResponse>.Success(new AgentLogResponse(id));
    }
}