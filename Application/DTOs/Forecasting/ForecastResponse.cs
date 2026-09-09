using System.Text.Json.Serialization;

namespace Application.DTOs.Forecasting;

public sealed record ForecastResponse(
    [property: JsonPropertyName("insufficient_data")] bool InsufficientData,
    [property: JsonPropertyName("dates")] IReadOnlyList<string> Dates,
    [property: JsonPropertyName("trend")] IReadOnlyList<double> Trend,
    [property: JsonPropertyName("yhat")] IReadOnlyList<double> Yhat,
    [property: JsonPropertyName("yhat_lower")] IReadOnlyList<double> YhatLower,
    [property: JsonPropertyName("yhat_upper")] IReadOnlyList<double> YhatUpper);