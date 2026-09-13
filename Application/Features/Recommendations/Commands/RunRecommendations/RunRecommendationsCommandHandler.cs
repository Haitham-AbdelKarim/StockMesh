using Application.Common.Models;
using Application.DTOs.Recommendations;
using Application.Features.Recommendations.Services;
using MediatR;

namespace Application.Features.Recommendations.Commands.RunRecommendations;

public sealed class RunRecommendationsCommandHandler :
    IRequestHandler<RunRecommendationsCommand, Result<RunSummaryResponse>>
{
    private readonly RecommendationGenerator _generator;

    public RunRecommendationsCommandHandler(RecommendationGenerator generator)
    {
        _generator = generator;
    }

    public async Task<Result<RunSummaryResponse>> Handle(
        RunRecommendationsCommand command,
        CancellationToken cancellationToken)
    {
        var summary = await _generator.RunAsync(cancellationToken);

        return Result<RunSummaryResponse>.Success(new RunSummaryResponse(
            summary.ProductsEvaluated,
            summary.RecommendationsWritten,
            summary.Skipped,
            summary.Failed));
    }
}