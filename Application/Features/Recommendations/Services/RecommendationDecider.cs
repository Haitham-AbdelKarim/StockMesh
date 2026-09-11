using Domain.Enums;

namespace Application.Features.Recommendations.Services;

public sealed record ForecastSeries(
    IReadOnlyList<string> Dates,
    IReadOnlyList<double> Trend,
    IReadOnlyList<double> Yhat,
    IReadOnlyList<double> YhatLower,
    IReadOnlyList<double> YhatUpper,
    int HistoryCount);

public sealed record ProductDecision(
    RecommendedAction Action,
    string Reason,
    double TrendSlope,
    bool AnomalyDetected,
    DateTime? PredictedDepletionDate,
    double ConfidenceScore);

public sealed record BatchDecision(
    RecommendedAction Action,
    string Reason,
    DateTime? PredictedDepletionDate);

public sealed record MarketAssessment(
    bool IsOpportunity,
    double TrendSlope,
    int ExceedanceDays);

public static class RecommendationDecider
{
    public const int AnomalyLookbackDays = 3;
    public const int AnomalyWindowDays = 7;
    public const int UrgencyWindowDays = 14;

    public static double LeastSquaresSlope(IReadOnlyList<double> values)
    {
        if (values.Count < 2)
        {
            return 0;
        }

        var meanX = (values.Count - 1) / 2.0;
        var meanY = values.Average();

        double numerator = 0;
        double denominator = 0;
        for (var i = 0; i < values.Count; i++)
        {
            numerator += (i - meanX) * (values[i] - meanY);
            denominator += (i - meanX) * (i - meanX);
        }

        return denominator == 0 ? 0 : numerator / denominator;
    }

    public static double ConfidenceScore(
        IReadOnlyList<double> yhat,
        IReadOnlyList<double> lower,
        IReadOnlyList<double> upper)
    {
        if (yhat.Count == 0 || lower.Count != yhat.Count || upper.Count != yhat.Count)
        {
            return 0;
        }

        var total = 0.0;
        for (var i = 0; i < yhat.Count; i++)
        {
            var width = Math.Max(upper[i] - lower[i], 0);
            total += width / Math.Max(Math.Abs(yhat[i]), 1e-9);
        }

        return Math.Clamp(1 - total / yhat.Count, 0, 1);
    }

    public static bool HasAnomaly(
        IReadOnlyList<double> actuals,
        IReadOnlyList<double> historyLower)
    {
        if (actuals.Count < AnomalyLookbackDays || historyLower.Count < AnomalyLookbackDays)
        {
            return false;
        }

        for (var k = 1; k <= AnomalyLookbackDays; k++)
        {
            if (!(actuals[^k] < historyLower[^k]))
            {
                return false;
            }
        }

        return true;
    }

    public static DateTime? DepletionDate(
        IReadOnlyList<string> dates,
        IReadOnlyList<double> yhat,
        int historyCount,
        double remaining)
    {
        var cumulative = 0.0;
        for (var i = historyCount; i < yhat.Count && i < dates.Count; i++)
        {
            cumulative += Math.Max(yhat[i], 0);
            if (cumulative >= remaining && DateTime.TryParse(dates[i], out var date))
            {
                return date.Date;
            }
        }

        return null;
    }

    public static double HorizonSum(
        IReadOnlyList<double> yhat,
        int historyCount,
        int days)
    {
        var sum = 0.0;
        for (var i = historyCount; i < Math.Min(historyCount + days, yhat.Count); i++)
        {
            sum += Math.Max(yhat[i], 0);
        }

        return sum;
    }

    public static ProductDecision DecideProduct(
        IReadOnlyList<double> actuals,
        ForecastSeries forecast,
        int leadTimeDays,
        double totalRemaining)
    {
        var slope = LeastSquaresSlope(forecast.Trend);
        var confidence = ConfidenceScore(forecast.Yhat, forecast.YhatLower, forecast.YhatUpper);
        var anomaly = HasAnomaly(actuals, forecast.YhatLower.Take(forecast.HistoryCount).ToList());
        var depletion = DepletionDate(
            forecast.Dates, forecast.Yhat, forecast.HistoryCount, totalRemaining);
        var projected = HorizonSum(forecast.Yhat, forecast.HistoryCount, leadTimeDays);

        if (projected >= totalRemaining)
        {
            var when = depletion?.ToString("yyyy-MM-dd") ?? "within the forecast horizon";
            return new ProductDecision(
                RecommendedAction.Reorder,
                $"Expected demand of {projected:F1} units over the next {leadTimeDays} days meets or exceeds {totalRemaining:F0} units on hand; predicted to deplete {when} at the current trend. Reorder soon.",
                slope,
                anomaly,
                depletion,
                confidence);
        }

        if (anomaly)
        {
            return new ProductDecision(
                RecommendedAction.Share,
                $"Sales ran below the forecast lower bound for the last {AnomalyLookbackDays} consecutive days, signalling a slump; expected demand of {projected:F1} units over the next {leadTimeDays} days stays within {totalRemaining:F0} units on hand. Consider sharing surplus with the network.",
                slope,
                anomaly,
                depletion,
                confidence);
        }

        return new ProductDecision(
            RecommendedAction.Hold,
            $"Expected demand of {projected:F1} units over the next {leadTimeDays} days is covered by {totalRemaining:F0} units on hand with no slump detected. No action needed.",
            slope,
            anomaly,
            depletion,
            confidence);
    }

    public static BatchDecision? DecideBatch(
        double batchQuantity,
        DateTime? expiryDate,
        DateOnly today,
        ForecastSeries forecast)
    {
        if (expiryDate is null)
        {
            return null;
        }

        var daysUntilExpiry = (expiryDate.Value.Date - today.ToDateTime(TimeOnly.MinValue)).Days;
        if (daysUntilExpiry <= 0)
        {
            return null;
        }

        var expectedSales = HorizonSum(forecast.Yhat, forecast.HistoryCount, daysUntilExpiry);
        if (expectedSales >= batchQuantity)
        {
            return null;
        }

        var depletion = DepletionDate(
            forecast.Dates, forecast.Yhat, forecast.HistoryCount, batchQuantity);

        if (daysUntilExpiry <= UrgencyWindowDays)
        {
            return new BatchDecision(
                RecommendedAction.UrgentShare,
                $"Batch expires on {expiryDate.Value:yyyy-MM-dd} ({daysUntilExpiry} days) but only {expectedSales:F1} units are expected to sell before then against {batchQuantity:F0} on hand. Share urgently before it expires.",
                depletion);
        }

        return new BatchDecision(
            RecommendedAction.Share,
            $"Only {expectedSales:F1} units are expected to sell before this batch expires on {expiryDate.Value:yyyy-MM-dd} against {batchQuantity:F0} on hand. Consider sharing the surplus with the network.",
            depletion);
    }

    public static MarketAssessment AssessMarket(
        IReadOnlyList<double> actuals,
        ForecastSeries forecast)
    {
        var slope = LeastSquaresSlope(forecast.Trend);

        var recentActuals = actuals.TakeLast(Math.Min(actuals.Count, AnomalyWindowDays)).ToList();
        var recentUpper = forecast.YhatUpper
            .Take(forecast.HistoryCount)
            .TakeLast(Math.Min(forecast.HistoryCount, AnomalyWindowDays))
            .ToList();

        var exceedances = 0;
        for (var i = 0; i < Math.Min(recentActuals.Count, recentUpper.Count); i++)
        {
            if (recentActuals[i] > recentUpper[i])
            {
                exceedances++;
            }
        }

        return new MarketAssessment(slope > 0 && exceedances >= 3, slope, exceedances);
    }

    public static (RecommendedAction Action, string Reason) Fallback(
        double averageDailySales,
        int leadTimeDays,
        double totalRemaining)
    {
        var projected = averageDailySales * leadTimeDays;
        if (projected >= totalRemaining)
        {
            return (
                RecommendedAction.Reorder,
                $"Fallback estimate: average sales of {averageDailySales:F1} units/day over the last 7 days project {projected:F1} units of demand over the next {leadTimeDays} days, exceeding {totalRemaining:F0} units on hand. Reorder soon.");
        }

        return (
            RecommendedAction.Hold,
            $"Fallback estimate: average sales of {averageDailySales:F1} units/day over the last 7 days project {projected:F1} units of demand over the next {leadTimeDays} days, covered by {totalRemaining:F0} units on hand. No action needed.");
    }
}