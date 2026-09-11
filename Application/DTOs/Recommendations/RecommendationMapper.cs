using Domain.Entities;

namespace Application.DTOs.Recommendations;

public static class RecommendationMapper
{
    public static RecommendationResponse ToResponse(
        Recommendation recommendation,
        string productName)
    {
        return new RecommendationResponse(
            recommendation.Id,
            recommendation.StoreId,
            recommendation.ProductId,
            productName,
            recommendation.BatchId,
            recommendation.RecommendedAction,
            recommendation.TrendSlope,
            recommendation.AnomalyDetected,
            recommendation.PredictedDepletionDate,
            recommendation.ConfidenceScore,
            recommendation.ModelVersion,
            recommendation.Reason,
            recommendation.GeneratedAt);
    }
}