using Application.Common.Models;
using Application.DTOs.Assistant;
using MediatR;

namespace Application.Features.Assistant.Queries.GetConversationById;

public sealed record GetConversationByIdQuery(Guid ConversationId) : IRequest<Result<ConversationDetailResponse>>;