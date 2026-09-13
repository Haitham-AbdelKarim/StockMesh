using Application.Common.Models;
using Application.DTOs.Recommendations;
using MediatR;

namespace Application.Features.Recommendations.Queries.GetRecommendationForProduct;

public sealed record GetRecommendationForProductQuery(
    Guid ProductId) : IRequest<Result<RecommendationResponse>>;