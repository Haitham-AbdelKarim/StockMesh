using Domain.Common;
using Domain.Enums;

namespace Domain.Entities;

public class Recommendation : BaseEntity
{
    public Guid StoreId { get; private set; }

    public Guid ProductId { get; private set; }

    public Guid? BatchId { get; private set; }

    public RecommendedAction RecommendedAction { get; private set; }

    public double TrendSlope { get; private set; }

    public bool AnomalyDetected { get; private set; }

    public DateTime? PredictedDepletionDate { get; private set; }

    public double ConfidenceScore { get; private set; }

    public string ModelVersion { get; private set; } = string.Empty;

    public string Reason { get; private set; } = string.Empty;

    public DateTime GeneratedAt { get; private set; }

    public string? ForecastSnapshotJson { get; private set; }

    private Recommendation()
    {
    }

    public Recommendation(
        Guid storeId,
        Guid productId,
        Guid? batchId,
        RecommendedAction recommendedAction,
        double trendSlope,
        bool anomalyDetected,
        DateTime? predictedDepletionDate,
        double confidenceScore,
        string modelVersion,
        string reason,
        DateTime generatedAt)
    {
        StoreId = storeId;
        ProductId = productId;
        BatchId = batchId;
        RecommendedAction = recommendedAction;
        TrendSlope = trendSlope;
        AnomalyDetected = anomalyDetected;
        PredictedDepletionDate = predictedDepletionDate;
        ConfidenceScore = confidenceScore;
        ModelVersion = modelVersion;
        Reason = reason;
        GeneratedAt = generatedAt;
    }

    public void SetForecastSnapshot(string? snapshotJson)
    {
        ForecastSnapshotJson = snapshotJson;
    }
}