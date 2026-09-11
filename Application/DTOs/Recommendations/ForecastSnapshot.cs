namespace Application.DTOs.Recommendations;

public sealed record ForecastSnapshot(
    IReadOnlyList<string> Dates,
    IReadOnlyList<double> Actuals,
    IReadOnlyList<double> Yhat,
    IReadOnlyList<double> Lower,
    IReadOnlyList<double> Upper);