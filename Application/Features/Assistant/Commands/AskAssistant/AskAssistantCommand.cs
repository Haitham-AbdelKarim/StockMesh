using Application.Common.Models;
using Application.DTOs.Assistant;
using MediatR;

namespace Application.Features.Assistant.Commands.AskAssistant;

public sealed record AskAssistantCommand(
    string Question,
    Guid? ConversationId = null) : IRequest<Result<AssistantStreamTicket>>;