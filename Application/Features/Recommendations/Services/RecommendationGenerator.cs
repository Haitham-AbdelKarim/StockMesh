using Application.Abstractions.Repositories;
using Application.Abstractions.Services;
using Application.DTOs.Recommendations;
using Domain.Entities;
using Domain.Enums;
using Microsoft.Extensions.Logging;

namespace Application.Features.Recommendations.Services;

public sealed record RunSummary(
    int ProductsEvaluated,
    int RecommendationsWritten,
    int Skipped,
    int Failed);

public sealed class RecommendationGenerator
{
    public const int HistoryDays = 180;
    public const int MinNonZeroPoints = 14;
    public const int MaxHorizonDays = 90;
    public const int FallbackLookbackDays = 7;

    private readonly IDateTimeProvider _clock;
    private readonly IStoreRepository _storeRepository;
    private readonly IDailyProductMetricRepository _productMetricRepository;
    private readonly IDailyMarketSignalRepository _marketSignalRepository;
    private readonly IInventoryBatchRepository _batchRepository;
    private readonly IProductRepository _productRepository;
    private readonly IForecastingClient _forecastingClient;
    private readonly IRecommendationRepository _recommendationRepository;
    private readonly ILogger<RecommendationGenerator> _logger;

    public RecommendationGenerator(
        IDateTimeProvider clock,
        IStoreRepository storeRepository,
        IDailyProductMetricRepository productMetricRepository,
        IDailyMarketSignalRepository marketSignalRepository,
        IInventoryBatchRepository batchRepository,
        IProductRepository productRepository,
        IForecastingClient forecastingClient,
        IRecommendationRepository recommendationRepository,
        ILogger<RecommendationGenerator> logger)
    {
        _clock = clock;
        _storeRepository = storeRepository;
        _productMetricRepository = productMetricRepository;
        _marketSignalRepository = marketSignalRepository;
        _batchRepository = batchRepository;
        _productRepository = productRepository;
        _forecastingClient = forecastingClient;
        _recommendationRepository = recommendationRepository;
        _logger = logger;
    }

    public async Task<RunSummary> RunAsync(CancellationToken cancellationToken = default)
    {
        var today = _clock.UtcNow.Date;
        var evaluated = 0;
        var written = 0;
        var skipped = 0;
        var failed = 0;

        var ownActions = new Dictionary<(Guid StoreId, Guid ProductId), bool>();

        foreach (var vertical in Enum.GetValues<VerticalCategory>())
        {
            var stores = await _storeRepository.GetByVerticalAsync(vertical, cancellationToken);
            foreach (var store in stores)
            {
                try
                {
                    var result = await ProcessStoreAsync(store.Id, today, ownActions, cancellationToken);
                    evaluated += result.Evaluated;
                    written += result.Written;
                    skipped += result.Skipped;
                }
                catch (Exception ex)
                {
                    failed++;
                    _logger.LogError(ex, "Recommendation pass failed for store {StoreId}.", store.Id);
                }
            }
        }

        foreach (var vertical in Enum.GetValues<VerticalCategory>())
        {
            try
            {
                written += await ProcessVerticalMarketAsync(vertical, today, ownActions, cancellationToken);
            }
            catch (Exception ex)
            {
                failed++;
                _logger.LogError(ex, "Market-opportunity pass failed for vertical {Vertical}.", vertical);
            }
        }

        return new RunSummary(evaluated, written, skipped, failed);
    }

    private async Task<(int Evaluated, int Written, int Skipped)> ProcessStoreAsync(
        Guid storeId,
        DateTime today,
        Dictionary<(Guid StoreId, Guid ProductId), bool> ownActions,
        CancellationToken cancellationToken)
    {
        var evaluated = 0;
        var written = 0;
        var skipped = 0;

        var metrics = await _productMetricRepository.GetRangeAsync(
            storeId, today.AddDays(-HistoryDays + 1), today.AddDays(1), cancellationToken);

        foreach (var group in metrics.GroupBy(m => m.ProductId))
        {
            evaluated++;
            try
            {
                var rows = await ProcessProductAsync(storeId, group.Key, group.ToList(), today, cancellationToken);
                if (rows is null)
                {
                    skipped++;
                    continue;
                }

                ownActions[(storeId, group.Key)] = rows.Decision.Action != RecommendedAction.Hold;
                written += rows.Count;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Recommendation pass failed for store {StoreId} product {ProductId}.",
                    storeId,
                    group.Key);
                skipped++;
            }
        }

        return (evaluated, written, skipped);
    }

    private sealed record EvaluatedProduct(
        ProductDecision Decision,
        string ModelVersion,
        IReadOnlyList<(InventoryBatch Batch, BatchDecision Decision)> BatchDecisions,
        string? ForecastSnapshotJson = null)
    {
        public int Count => 1 + BatchDecisions.Count;
    }

    private async Task<EvaluatedProduct?> ProcessProductAsync(
        Guid storeId,
        Guid productId,
        List<DailyProductMetric> rows,
        DateTime today,
        CancellationToken cancellationToken)
    {
        var batches = (await _batchRepository.GetByProductAsync(productId, cancellationToken))
            .Where(b => b.StoreId == storeId)
            .ToList();
        var totalRemaining = batches.Sum(b => (double)(b.QuantityRemaining + b.SharedQuantity));
        var leadTime = batches.Count == 0 ? 0 : batches.Max(b => b.LeadTimeDays);

        var start = today.AddDays(-HistoryDays + 1);
        var byDate = rows
            .Where(m => m.Date.Date >= start)
            .GroupBy(m => m.Date.Date)
            .ToDictionary(g => g.Key, g => (double)g.Sum(m => m.UnitsSold));
        var dates = Enumerable.Range(0, HistoryDays).Select(i => start.AddDays(i)).ToList();
        var actuals = dates.Select(d => byDate.GetValueOrDefault(d, 0.0)).ToList();

        var maxExpiryWindow = batches
            .Where(b => b.ExpiryDate is not null)
            .Select(b => Math.Max((b.ExpiryDate!.Value.Date - today).Days, 0))
            .DefaultIfEmpty(0)
            .Max();
        var horizon = Math.Clamp(Math.Max(Math.Max(leadTime, maxExpiryWindow), 14), 1, MaxHorizonDays);
        var now = _clock.UtcNow;

        if (actuals.Count(v => v != 0) < MinNonZeroPoints)
        {
            var fallback = BuildFallback(actuals, leadTime, totalRemaining);
            var evaluatedFallback = new EvaluatedProduct(fallback.Decision, "fallback-v1", []);
            await PersistAsync(storeId, productId, evaluatedFallback, now, cancellationToken);

            return evaluatedFallback;
        }

        var series = dates
            .Select((d, i) => new DailyPoint(DateOnly.FromDateTime(d), actuals[i]))
            .ToList();
        var forecast = await _forecastingClient.ForecastAsync(series, horizon, cancellationToken);
        if (forecast is null)
        {
            return null;
        }

        if (forecast.InsufficientData)
        {
            var fallback = BuildFallback(actuals, leadTime, totalRemaining);
            var evaluatedFallback = new EvaluatedProduct(fallback.Decision, "fallback-v1", []);
            await PersistAsync(storeId, productId, evaluatedFallback, now, cancellationToken);

            return evaluatedFallback;
        }

        var inputs = new ForecastSeries(
            forecast.Dates, forecast.Trend, forecast.Yhat, forecast.YhatLower, forecast.YhatUpper, series.Count);
        var decision = RecommendationDecider.DecideProduct(actuals, inputs, leadTime, totalRemaining);

        var batchDecisions = new List<(InventoryBatch Batch, BatchDecision Decision)>();
        var todayDate = DateOnly.FromDateTime(today);
        foreach (var batch in batches)
        {
            var batchDecision = RecommendationDecider.DecideBatch(
                batch.QuantityRemaining, batch.ExpiryDate, todayDate, inputs);
            if (batchDecision is not null)
            {
                batchDecisions.Add((batch, batchDecision));
            }
        }

        // The product row carries the highest severity across the product-level
        // rule and every batch-level rule; batch rows keep their own detail.
        var effective = decision;
        var strongest = batchDecisions
            .OrderByDescending(b => Severity(b.Decision.Action))
            .FirstOrDefault();
        if (strongest.Decision is not null && Severity(strongest.Decision.Action) > Severity(decision.Action))
        {
            effective = decision with { Action = strongest.Decision.Action, Reason = strongest.Decision.Reason };
        }

        var evaluated = new EvaluatedProduct(
            effective,
            "prophet-v1",
            batchDecisions,
            RecommendationMapper.SerializeSnapshot(new ForecastSnapshot(
                forecast.Dates,
                actuals,
                forecast.Yhat,
                forecast.YhatLower,
                forecast.YhatUpper)));
        await PersistAsync(storeId, productId, evaluated, now, cancellationToken);

        return evaluated;
    }

    private static int Severity(RecommendedAction action)
    {
        return action switch
        {
            RecommendedAction.UrgentShare => 4,
            RecommendedAction.Reorder => 3,
            RecommendedAction.Share => 2,
            RecommendedAction.MarketOpportunity => 1,
            _ => 0
        };
    }

    private static EvaluatedProduct BuildFallback(
        List<double> actuals,
        int leadTime,
        double totalRemaining)
    {
        var average = actuals.TakeLast(Math.Min(actuals.Count, FallbackLookbackDays)).DefaultIfEmpty(0).Average();
        var (action, reason) = RecommendationDecider.Fallback(average, leadTime, totalRemaining);
        var decision = new ProductDecision(action, reason, 0, false, null, 0);

        return new EvaluatedProduct(decision, "fallback-v1", []);
    }

    private async Task<int> ProcessVerticalMarketAsync(
        VerticalCategory vertical,
        DateTime today,
        Dictionary<(Guid StoreId, Guid ProductId), bool> ownActions,
        CancellationToken cancellationToken)
    {
        var written = 0;
        var stores = await _storeRepository.GetByVerticalAsync(vertical, cancellationToken);
        if (stores.Count == 0)
        {
            return written;
        }

        const int pageSize = 100;
        var page = 1;
        while (true)
        {
            var (items, totalCount) = await _productRepository.GetAsync(
                vertical, null, page, pageSize, cancellationToken);

            foreach (var product in items)
            {
                try
                {
                    written += await ProcessMarketProductAsync(
                        product, stores.Select(s => s.Id).ToList(), today, ownActions, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Market-opportunity pass failed for product {ProductId}.",
                        product.Id);
                }
            }

            if (page * pageSize >= totalCount)
            {
                break;
            }

            page++;
        }

        return written;
    }

    private async Task<int> ProcessMarketProductAsync(
        Product product,
        List<Guid> storeIds,
        DateTime today,
        Dictionary<(Guid StoreId, Guid ProductId), bool> ownActions,
        CancellationToken cancellationToken)
    {
        var start = today.AddDays(-HistoryDays + 1);
        var history = await _marketSignalRepository.GetHistoryAsync(
            product.Id, product.VerticalCategory, start, today.AddDays(1), cancellationToken);
        if (history.Count == 0)
        {
            return 0;
        }

        var byDate = history.ToDictionary(s => s.Date.Date, s => (double)s.TransferVolume);
        var dates = Enumerable.Range(0, HistoryDays).Select(i => start.AddDays(i)).ToList();
        var actuals = dates.Select(d => byDate.GetValueOrDefault(d, 0.0)).ToList();

        if (actuals.Count(v => v != 0) < MinNonZeroPoints)
        {
            return 0;
        }

        var series = dates
            .Select((d, i) => new DailyPoint(DateOnly.FromDateTime(d), actuals[i]))
            .ToList();
        var forecast = await _forecastingClient.ForecastAsync(series, 14, cancellationToken);
        if (forecast is null || forecast.InsufficientData)
        {
            return 0;
        }

        var inputs = new ForecastSeries(
            forecast.Dates, forecast.Trend, forecast.Yhat, forecast.YhatLower, forecast.YhatUpper, series.Count);
        var assessment = RecommendationDecider.AssessMarket(actuals, inputs);
        if (!assessment.IsOpportunity)
        {
            return 0;
        }

        var confidence = RecommendationDecider.ConfidenceScore(
            forecast.Yhat, forecast.YhatLower, forecast.YhatUpper);
        var now = _clock.UtcNow;
        var written = 0;

        foreach (var storeId in storeIds)
        {
            // Market opportunity only upgrades a Hold: own survival signals win.
            if (!ownActions.TryGetValue((storeId, product.Id), out var acted) || acted)
            {
                continue;
            }

            var existing = await _recommendationRepository.GetForProductAsync(
                storeId, product.Id, cancellationToken);
            var productRow = existing.FirstOrDefault(r => r.BatchId is null);
            if (productRow is not null)
            {
                _recommendationRepository.RemoveRange([productRow]);
            }

            var marketRow = new Recommendation(
                storeId,
                product.Id,
                null,
                RecommendedAction.MarketOpportunity,
                assessment.TrendSlope,
                false,
                null,
                confidence,
                "prophet-v1",
                $"Network demand for this product is rising (trend slope {assessment.TrendSlope:F2} units/day) with {assessment.ExceedanceDays} of the last 7 days above the forecast upper bound across the {product.VerticalCategory} vertical. Consider stocking more to capture the opportunity.",
                now);
            marketRow.SetForecastSnapshot(RecommendationMapper.SerializeSnapshot(new ForecastSnapshot(
                forecast.Dates,
                actuals,
                forecast.Yhat,
                forecast.YhatLower,
                forecast.YhatUpper)));
            await _recommendationRepository.AddAsync(marketRow, cancellationToken);

            written++;
        }

        await _recommendationRepository.SaveChangesAsync(cancellationToken);

        return written;
    }

    private async Task PersistAsync(
        Guid storeId,
        Guid productId,
        EvaluatedProduct evaluated,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var existing = await _recommendationRepository.GetForProductAsync(
            storeId, productId, cancellationToken);
        _recommendationRepository.RemoveRange(existing);

        var productRow = new Recommendation(
            storeId,
            productId,
            null,
            evaluated.Decision.Action,
            evaluated.Decision.TrendSlope,
            evaluated.Decision.AnomalyDetected,
            evaluated.Decision.PredictedDepletionDate,
            evaluated.Decision.ConfidenceScore,
            evaluated.ModelVersion,
            evaluated.Decision.Reason,
            now);
        productRow.SetForecastSnapshot(evaluated.ForecastSnapshotJson);
        await _recommendationRepository.AddAsync(productRow, cancellationToken);

        foreach (var (batch, batchDecision) in evaluated.BatchDecisions)
        {
            var batchRow = new Recommendation(
                storeId,
                productId,
                batch.Id,
                batchDecision.Action,
                evaluated.Decision.TrendSlope,
                evaluated.Decision.AnomalyDetected,
                batchDecision.PredictedDepletionDate,
                evaluated.Decision.ConfidenceScore,
                evaluated.ModelVersion,
                batchDecision.Reason,
                now);
            batchRow.SetForecastSnapshot(evaluated.ForecastSnapshotJson);
            await _recommendationRepository.AddAsync(batchRow, cancellationToken);
        }

        await _recommendationRepository.SaveChangesAsync(cancellationToken);
    }
}