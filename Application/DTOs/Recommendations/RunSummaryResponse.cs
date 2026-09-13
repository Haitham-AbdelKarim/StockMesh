namespace Application.DTOs.Recommendations;

public sealed record RunSummaryResponse(
    int ProductsEvaluated,
    int RecommendationsWritten,
    int Skipped,
    int Failed);