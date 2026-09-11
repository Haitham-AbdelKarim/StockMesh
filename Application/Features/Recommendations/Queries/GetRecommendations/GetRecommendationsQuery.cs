using Application.Common.Models;
using Application.DTOs.Recommendations;
using Domain.Enums;
using MediatR;

namespace Application.Features.Recommendations.Queries.GetRecommendations;

public sealed record GetRecommendationsQuery(
    RecommendedAction? RecommendedAction = null,
    int Page = 1,
    int PageSize = 20) : IRequest<Result<PaginatedList<RecommendationResponse>>>;