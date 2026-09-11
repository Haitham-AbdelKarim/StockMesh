using Domain.Enums;

namespace Application.DTOs.Recommendations;

public sealed record RecommendationResponse(
    Guid Id,
    Guid StoreId,
    Guid ProductId,
    string ProductName,
    Guid? BatchId,
    RecommendedAction RecommendedAction,
    double TrendSlope,
    bool AnomalyDetected,
    DateTime? PredictedDepletionDate,
    double ConfidenceScore,
    string ModelVersion,
    string Reason,
    DateTime GeneratedAt);