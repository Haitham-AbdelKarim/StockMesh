using Application.Features.Recommendations.Services;
using Domain.Enums;
using FluentAssertions;

namespace StockMesh.Application.UnitTests.Features.Recommendations;

public class RecommendationDeciderTests
{
    private static ForecastSeries Forecast(
        int history,
        int horizon,
        double[]? trend = null,
        double[]? yhat = null,
        double[]? lower = null,
        double[]? upper = null)
    {
        var total = history + horizon;
        var dates = Enumerable.Range(0, total)
            .Select(i => new DateOnly(2026, 1, 1).AddDays(i).ToString("yyyy-MM-dd"))
            .ToList();

        double[] Fill(double[]? values, double fallback)
        {
            return values ?? Enumerable.Repeat(fallback, total).ToArray();
        }

        return new ForecastSeries(
            dates,
            Fill(trend, 1.0),
            Fill(yhat, 5.0),
            Fill(lower, 3.0),
            Fill(upper, 7.0),
            history);
    }

    private static List<double> Actuals(params double[] values)
    {
        return values.ToList();
    }

    [Fact]
    public void DecideProduct_ProjectedDemandMeetsStock_ReturnsReorder()
    {
        var actuals = Actuals(Enumerable.Repeat(5.0, 20).ToArray());
        var forecast = Forecast(20, 10);

        var decision = RecommendationDecider.DecideProduct(actuals, forecast, 10, 40);

        decision.Action.Should().Be(RecommendedAction.Reorder);
        decision.PredictedDepletionDate.Should().NotBeNull();
        decision.Reason.Should().Contain("Reorder soon");
        decision.Reason.Should().Contain("50.0");
        decision.ConfidenceScore.Should().BeInRange(0, 1);
    }

    [Fact]
    public void DecideProduct_CoveredDemandWithoutSlump_ReturnsHold()
    {
        var actuals = Actuals(Enumerable.Repeat(5.0, 20).ToArray());
        var forecast = Forecast(20, 10);

        var decision = RecommendationDecider.DecideProduct(actuals, forecast, 10, 100);

        decision.Action.Should().Be(RecommendedAction.Hold);
        decision.AnomalyDetected.Should().BeFalse();
        decision.Reason.Should().Contain("No action needed");
    }

    [Fact]
    public void DecideProduct_ThreeDaysBelowLowerBound_ReturnsShareWithAnomaly()
    {
        var values = Enumerable.Repeat(5.0, 17).Concat(new[] { 0.0, 1.0, 2.0 }).ToArray();
        var actuals = Actuals(values);
        var forecast = Forecast(20, 10);

        var decision = RecommendationDecider.DecideProduct(actuals, forecast, 10, 100);

        decision.Action.Should().Be(RecommendedAction.Share);
        decision.AnomalyDetected.Should().BeTrue();
        decision.Reason.Should().Contain("slump");
        decision.Reason.Should().Contain("3 consecutive days");
    }

    [Fact]
    public void DecideProduct_TwoDaysBelowLowerBound_IsNotAnomaly()
    {
        var values = Enumerable.Repeat(5.0, 18).Concat(new[] { 0.0, 1.0 }).ToArray();
        var actuals = Actuals(values);
        var forecast = Forecast(20, 10);

        var decision = RecommendationDecider.DecideProduct(actuals, forecast, 10, 100);

        decision.Action.Should().Be(RecommendedAction.Hold);
        decision.AnomalyDetected.Should().BeFalse();
    }

    [Fact]
    public void DecideBatch_ExpiringWithinFourteenDays_ReturnsUrgentShare()
    {
        var forecast = Forecast(20, 10, yhat: Enumerable.Repeat(1.0, 30).ToArray());
        var today = new DateOnly(2026, 1, 21);

        var decision = RecommendationDecider.DecideBatch(
            10, new DateTime(2026, 1, 26), today, forecast);

        decision.Should().NotBeNull();
        decision!.Action.Should().Be(RecommendedAction.UrgentShare);
        decision.Reason.Should().Contain("urgently");
        decision.Reason.Should().Contain("2026-01-26");
    }

    [Fact]
    public void DecideBatch_ExpiringBeyondFourteenDays_ReturnsShare()
    {
        var forecast = Forecast(20, 30, yhat: Enumerable.Repeat(1.0, 50).ToArray());
        var today = new DateOnly(2026, 1, 21);

        var decision = RecommendationDecider.DecideBatch(
            50, new DateTime(2026, 2, 20), today, forecast);

        decision.Should().NotBeNull();
        decision!.Action.Should().Be(RecommendedAction.Share);
    }

    [Fact]
    public void DecideBatch_WithoutExpiry_ReturnsNull()
    {
        var forecast = Forecast(20, 10);
        var today = new DateOnly(2026, 1, 21);

        RecommendationDecider.DecideBatch(10, null, today, forecast).Should().BeNull();
    }

    [Fact]
    public void DecideBatch_ExpectedSalesCoverStock_ReturnsNull()
    {
        var forecast = Forecast(20, 10, yhat: Enumerable.Repeat(5.0, 30).ToArray());
        var today = new DateOnly(2026, 1, 21);

        RecommendationDecider.DecideBatch(10, new DateTime(2026, 2, 20), today, forecast)
            .Should().BeNull();
    }

    [Fact]
    public void AssessMarket_RisingTrendWithExceedances_IsOpportunity()
    {
        var trend = Enumerable.Range(1, 17).Select(i => (double)i).ToArray();
        var forecast = Forecast(
            10, 7,
            trend: trend,
            yhat: Enumerable.Repeat(5.0, 17).ToArray(),
            lower: Enumerable.Repeat(3.0, 17).ToArray(),
            upper: Enumerable.Repeat(6.0, 17).ToArray());
        var actuals = Actuals(5, 5, 5, 5, 5, 5, 5, 9, 9, 9);

        var assessment = RecommendationDecider.AssessMarket(actuals, forecast);

        assessment.IsOpportunity.Should().BeTrue();
        assessment.TrendSlope.Should().BeGreaterThan(0);
        assessment.ExceedanceDays.Should().Be(3);
    }

    [Fact]
    public void AssessMarket_FlatTrend_IsNotOpportunity()
    {
        var forecast = Forecast(
            10, 7,
            yhat: Enumerable.Repeat(5.0, 17).ToArray(),
            upper: Enumerable.Repeat(4.0, 17).ToArray());
        var actuals = Actuals(5, 5, 5, 5, 5, 5, 5, 9, 9, 9);

        RecommendationDecider.AssessMarket(actuals, forecast).IsOpportunity.Should().BeFalse();
    }

    [Fact]
    public void AssessMarket_TooFewExceedances_IsNotOpportunity()
    {
        var trend = Enumerable.Range(1, 17).Select(i => (double)i).ToArray();
        var forecast = Forecast(
            10, 7,
            trend: trend,
            yhat: Enumerable.Repeat(5.0, 17).ToArray(),
            upper: Enumerable.Repeat(6.0, 17).ToArray());
        var actuals = Actuals(5, 5, 5, 5, 5, 5, 5, 5, 5, 9);

        var assessment = RecommendationDecider.AssessMarket(actuals, forecast);

        assessment.IsOpportunity.Should().BeFalse();
        assessment.ExceedanceDays.Should().Be(1);
    }

    [Fact]
    public void Fallback_ProjectedDemandExceedsStock_ReturnsReorder()
    {
        var (action, reason) = RecommendationDecider.Fallback(5.0, 10, 40);

        action.Should().Be(RecommendedAction.Reorder);
        reason.Should().Contain("Fallback estimate");
        reason.Should().Contain("50.0");
    }

    [Fact]
    public void Fallback_CoveredDemand_ReturnsHold()
    {
        var (action, reason) = RecommendationDecider.Fallback(5.0, 10, 100);

        action.Should().Be(RecommendedAction.Hold);
        reason.Should().Contain("Fallback estimate");
    }

    [Fact]
    public void LeastSquaresSlope_RisingSeries_IsPositive()
    {
        RecommendationDecider.LeastSquaresSlope(new[] { 1.0, 2.0, 3.0, 4.0 })
            .Should().BeApproximately(1.0, 1e-9);
    }

    [Fact]
    public void LeastSquaresSlope_FlatOrSinglePoint_IsZero()
    {
        RecommendationDecider.LeastSquaresSlope(new[] { 5.0, 5.0, 5.0 }).Should().Be(0);
        RecommendationDecider.LeastSquaresSlope(new[] { 5.0 }).Should().Be(0);
    }

    [Fact]
    public void ConfidenceScore_PerfectInterval_IsOne()
    {
        RecommendationDecider.ConfidenceScore(
            new[] { 5.0, 5.0 }, new[] { 5.0, 5.0 }, new[] { 5.0, 5.0 })
            .Should().Be(1);
    }

    [Fact]
    public void ConfidenceScore_EmptySeries_IsZero()
    {
        RecommendationDecider.ConfidenceScore([], [], []).Should().Be(0);
    }

    [Fact]
    public void DepletionDate_NeverDepletes_ReturnsNull()
    {
        var dates = Enumerable.Range(0, 10)
            .Select(i => new DateOnly(2026, 1, 1).AddDays(i).ToString("yyyy-MM-dd"))
            .ToList();

        RecommendationDecider.DepletionDate(dates, Enumerable.Repeat(1.0, 10).ToList(), 0, 1000)
            .Should().BeNull();
    }

    [Fact]
    public void DepletionDate_CumulativeCrossing_ReturnsFirstCrossingDate()
    {
        var dates = Enumerable.Range(0, 10)
            .Select(i => new DateOnly(2026, 1, 1).AddDays(i).ToString("yyyy-MM-dd"))
            .ToList();

        RecommendationDecider.DepletionDate(dates, Enumerable.Repeat(5.0, 10).ToList(), 0, 12)
            .Should().Be(new DateTime(2026, 1, 3));
    }
}