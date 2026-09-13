using Application.Common.Models;
using Application.DTOs.Assistant;
using MediatR;

namespace Application.Features.Assistant.Queries.GetConversations;

public sealed record GetConversationsQuery : IRequest<Result<IReadOnlyList<ConversationResponse>>>;