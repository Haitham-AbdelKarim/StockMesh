using Application.Common.Models;
using Application.DTOs.Recommendations;
using MediatR;

namespace Application.Features.Recommendations.Commands.RunRecommendations;

public sealed record RunRecommendationsCommand : IRequest<Result<RunSummaryResponse>>;