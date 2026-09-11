using System.Text.Json;
using Domain.Entities;

namespace Application.DTOs.Recommendations;

public static class RecommendationMapper
{
    private static readonly JsonSerializerOptions SnapshotJson = new(JsonSerializerDefaults.Web);

    public static RecommendationResponse ToResponse(
        Recommendation recommendation,
        string productName)
    {
        ForecastSnapshot? forecast = null;
        if (recommendation.ForecastSnapshotJson is { Length: > 0 } json)
        {
            try
            {
                forecast = JsonSerializer.Deserialize<ForecastSnapshot>(json, SnapshotJson);
            }
            catch (JsonException)
            {
                forecast = null;
            }
        }

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
            recommendation.GeneratedAt,
            forecast);
    }

    public static string SerializeSnapshot(ForecastSnapshot snapshot)
    {
        return JsonSerializer.Serialize(snapshot, SnapshotJson);
    }
}