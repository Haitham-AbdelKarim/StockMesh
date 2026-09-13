using Application.Common.Models;
using Application.DTOs.AgentLogs;
using MediatR;

namespace Application.Features.AgentLogs.Commands.RecordAgentLog;

public sealed record RecordAgentLogCommand(
    Guid StoreId,
    Guid UserId,
    Guid ConversationId,
    string Question,
    string ToolCalls,
    string FinalAnswer) : IRequest<Result<AgentLogResponse>>;