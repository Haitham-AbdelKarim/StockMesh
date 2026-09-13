using Application.DTOs.Forecasting;
using Application.Features.Recommendations.Services;
using Domain.Entities;
using Domain.Enums;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using StockMesh.Application.UnitTests.Fakes;

namespace StockMesh.Application.UnitTests.Features.Recommendations;

public class RecommendationGeneratorTests
{
    private static readonly Product GamingProduct =
        new("PlayStation 5 Console", VerticalCategory.Gaming, "Sony");

    private static ForecastResponse FlatForecast(int history, int horizon)
    {
        var total = history + horizon;
        var dates = Enumerable.Range(0, total)
            .Select(i => new DateOnly(2026, 1, 1).AddDays(i).ToString("yyyy-MM-dd"))
            .ToList();

        return new ForecastResponse(
            false,
            dates,
            Enumerable.Repeat(1.0, total).ToList(),
            Enumerable.Repeat(5.0, total).ToList(),
            Enumerable.Repeat(3.0, total).ToList(),
            Enumerable.Repeat(7.0, total).ToList());
    }

    private static List<DailyProductMetric> SalesHistory(
        Guid storeId,
        Guid productId,
        DateTime today,
        int days,
        int unitsPerDay,
        int transfersPerDay = 0)
    {
        var rows = new List<DailyProductMetric>();
        for (var i = 0; i < days; i++)
        {
            var metric = new DailyProductMetric(storeId, productId, today.AddDays(-i));
            metric.Update(unitsPerDay, unitsPerDay * 10m, unitsPerDay * 5m, transfersPerDay, transfersPerDay * 10m);
            rows.Add(metric);
        }

        return rows;
    }

    private static Store GamingStore(string name)
    {
        return new Store(name, VerticalCategory.Gaming, 30.05, 31.25, 30);
    }

    [Fact]
    public async Task RunAsync_ForecastBackedReorder_WritesProphetRowAndReplacesOnRerun()
    {
        var clock = new FakeDateTimeProvider { UtcNow = new DateTime(2026, 6, 1, 12, 0, 0, DateTimeKind.Utc) };
        var today = clock.UtcNow.Date;
        var store = GamingStore("Store A");
        var batch = new InventoryBatch(
            store.Id, GamingProduct.Id, 10, 5m, 10m, reorderPoint: 5, leadTimeDays: 2, expiryDate: null);
        var metrics = SalesHistory(store.Id, GamingProduct.Id, today, 20, 5);
        var signals = new FakeDailyMarketSignalRepository();
        var recommendations = new FakeRecommendationRepository();
        var client = new FakeForecastingClient((series, horizon) => FlatForecast(series.Count, horizon));
        var generator = CreateGenerator(
            clock,
            new FakeStoreRepository(store),
            new FakeDailyProductMetricRepository(metrics.ToArray()),
            signals,
            new FakeInventoryBatchRepository(batch),
            new FakeProductRepository(GamingProduct),
            client,
            recommendations);

        var first = await generator.RunAsync(CancellationToken.None);
        var second = await generator.RunAsync(CancellationToken.None);

        first.ProductsEvaluated.Should().Be(1);
        first.Failed.Should().Be(0);

        var row = recommendations.All.Should().ContainSingle().Subject;
        row.StoreId.Should().Be(store.Id);
        row.ProductId.Should().Be(GamingProduct.Id);
        row.BatchId.Should().BeNull();
        row.RecommendedAction.Should().Be(RecommendedAction.Reorder);
        row.ModelVersion.Should().Be("prophet-v1");
        row.Reason.Should().Contain("Reorder soon");

        second.ProductsEvaluated.Should().Be(1);
        recommendations.All.Should().ContainSingle();
    }

    [Fact]
    public async Task RunAsync_InsufficientData_FallsBackWithoutCallingFurtherAttention()
    {
        var clock = new FakeDateTimeProvider { UtcNow = new DateTime(2026, 6, 1, 12, 0, 0, DateTimeKind.Utc) };
        var today = clock.UtcNow.Date;
        var store = GamingStore("Store A");
        var batch = new InventoryBatch(
            store.Id, GamingProduct.Id, 10, 5m, 10m, reorderPoint: 5, leadTimeDays: 2, expiryDate: null);
        var metrics = SalesHistory(store.Id, GamingProduct.Id, today, 20, 5);
        var recommendations = new FakeRecommendationRepository();
        var client = new FakeForecastingClient((_, _) => new ForecastResponse(
            true, [], [], [], [], []));
        var generator = CreateGenerator(
            clock,
            new FakeStoreRepository(store),
            new FakeDailyProductMetricRepository(metrics.ToArray()),
            new FakeDailyMarketSignalRepository(),
            new FakeInventoryBatchRepository(batch),
            new FakeProductRepository(GamingProduct),
            client,
            recommendations);

        await generator.RunAsync(CancellationToken.None);

        var row = recommendations.All.Should().ContainSingle().Subject;
        row.RecommendedAction.Should().Be(RecommendedAction.Reorder);
        row.ModelVersion.Should().Be("fallback-v1");
        row.Reason.Should().Contain("Fallback estimate");
    }

    [Fact]
    public async Task RunAsync_NullForecastResponse_SkipsProduct()
    {
        var clock = new FakeDateTimeProvider { UtcNow = new DateTime(2026, 6, 1, 12, 0, 0, DateTimeKind.Utc) };
        var today = clock.UtcNow.Date;
        var store = GamingStore("Store A");
        var batch = new InventoryBatch(store.Id, GamingProduct.Id, 10, 5m, 10m);
        var metrics = SalesHistory(store.Id, GamingProduct.Id, today, 20, 5);
        var recommendations = new FakeRecommendationRepository();
        var client = new FakeForecastingClient((_, _) => null);
        var generator = CreateGenerator(
            clock,
            new FakeStoreRepository(store),
            new FakeDailyProductMetricRepository(metrics.ToArray()),
            new FakeDailyMarketSignalRepository(),
            new FakeInventoryBatchRepository(batch),
            new FakeProductRepository(GamingProduct),
            client,
            recommendations);

        var summary = await generator.RunAsync(CancellationToken.None);

        recommendations.All.Should().BeEmpty();
        summary.Skipped.Should().Be(1);
    }

    [Fact]
    public async Task RunAsync_SeriesSentToForecaster_ContainsOnlySaleQuantities()
    {
        var clock = new FakeDateTimeProvider { UtcNow = new DateTime(2026, 6, 1, 12, 0, 0, DateTimeKind.Utc) };
        var today = clock.UtcNow.Date;
        var store = GamingStore("Store A");
        var batch = new InventoryBatch(store.Id, GamingProduct.Id, 100, 5m, 10m);
        // Transfers recorded on the same days must never leak into the demand series.
        var metrics = SalesHistory(store.Id, GamingProduct.Id, today, 20, 5, transfersPerDay: 99);
        var client = new FakeForecastingClient((series, horizon) => FlatForecast(series.Count, horizon));
        var generator = CreateGenerator(
            clock,
            new FakeStoreRepository(store),
            new FakeDailyProductMetricRepository(metrics.ToArray()),
            new FakeDailyMarketSignalRepository(),
            new FakeInventoryBatchRepository(batch),
            new FakeProductRepository(GamingProduct),
            client,
            new FakeRecommendationRepository());

        await generator.RunAsync(CancellationToken.None);

        var sent = client.Calls.Should().ContainSingle().Subject;
        sent.Series.Should().HaveCount(180);
        sent.Series.Should().OnlyContain(p => p.Value == 5 || p.Value == 0);
        sent.Series.TakeLast(20).Should().OnlyContain(p => p.Value == 5);
        sent.Series.Should().NotContain(p => p.Value == 99);
    }

    [Fact]
    public async Task RunAsync_ExpiringBatch_WritesUrgentShareRows()
    {
        var clock = new FakeDateTimeProvider { UtcNow = new DateTime(2026, 6, 1, 12, 0, 0, DateTimeKind.Utc) };
        var today = clock.UtcNow.Date;
        var store = GamingStore("Store A");
        var batch = new InventoryBatch(
            store.Id, GamingProduct.Id, 10, 5m, 10m,
            reorderPoint: 0, leadTimeDays: 0, expiryDate: today.AddDays(5));
        var metrics = SalesHistory(store.Id, GamingProduct.Id, today, 20, 5);
        var recommendations = new FakeRecommendationRepository();
        var client = new FakeForecastingClient((series, horizon) =>
        {
            var total = series.Count + horizon;
            var dates = series.Select(p => p.Date.ToString("yyyy-MM-dd"))
                .Concat(Enumerable.Range(0, horizon).Select(i =>
                    series[^1].Date.AddDays(i + 1).ToString("yyyy-MM-dd")))
                .ToList();
            return new ForecastResponse(
                false,
                dates,
                Enumerable.Repeat(1.0, total).ToList(),
                Enumerable.Repeat(1.0, total).ToList(),
                Enumerable.Repeat(0.0, total).ToList(),
                Enumerable.Repeat(2.0, total).ToList());
        });
        var generator = CreateGenerator(
            clock,
            new FakeStoreRepository(store),
            new FakeDailyProductMetricRepository(metrics.ToArray()),
            new FakeDailyMarketSignalRepository(),
            new FakeInventoryBatchRepository(batch),
            new FakeProductRepository(GamingProduct),
            client,
            recommendations);

        await generator.RunAsync(CancellationToken.None);

        recommendations.All.Should().HaveCount(2);
        recommendations.All.Should().OnlyContain(r => r.RecommendedAction == RecommendedAction.UrgentShare);
        recommendations.All.Should().ContainSingle(r => r.BatchId == batch.Id);
        recommendations.All.Should().ContainSingle(r => r.BatchId == null);
    }

    [Fact]
    public async Task RunAsync_RisingMarket_UpgradesHoldToMarketOpportunity()
    {
        var clock = new FakeDateTimeProvider { UtcNow = new DateTime(2026, 6, 1, 12, 0, 0, DateTimeKind.Utc) };
        var today = clock.UtcNow.Date;
        var store = GamingStore("Store A");
        var batch = new InventoryBatch(store.Id, GamingProduct.Id, 100, 5m, 10m);
        var metrics = SalesHistory(store.Id, GamingProduct.Id, today, 20, 5);

        var signals = new List<DailyMarketSignal>();
        for (var i = 0; i < 20; i++)
        {
            var signal = new DailyMarketSignal(GamingProduct.Id, VerticalCategory.Gaming, today.AddDays(-i));
            signal.Update(2, i + 1, 2);
            signals.Add(signal);
        }

        var recommendations = new FakeRecommendationRepository();
        var client = new FakeForecastingClient((series, horizon) =>
        {
            // Flat demand for the own-series call, echoing market shape otherwise.
            if (series.Max(p => p.Value) <= 5)
            {
                return FlatForecast(series.Count, horizon);
            }

            var values = series.Select(p => p.Value).ToList();
            var total = series.Count + horizon;
            var dates = series.Select(p => p.Date.ToString("yyyy-MM-dd"))
                .Concat(Enumerable.Range(0, horizon).Select(i =>
                    series[^1].Date.AddDays(i + 1).ToString("yyyy-MM-dd")))
                .ToList();
            return new ForecastResponse(
                false,
                dates,
                Enumerable.Range(1, total).Select(i => (double)i).ToList(),
                values.Concat(Enumerable.Repeat(values[^1], horizon)).ToList(),
                values.Select(v => v - 3).Concat(Enumerable.Repeat(values[^1] - 3, horizon)).ToList(),
                values.Select(v => v - 1).Concat(Enumerable.Repeat(values[^1] - 1, horizon)).ToList());
        });
        var generator = CreateGenerator(
            clock,
            new FakeStoreRepository(store),
            new FakeDailyProductMetricRepository(metrics.ToArray()),
            new FakeDailyMarketSignalRepository(signals.ToArray()),
            new FakeInventoryBatchRepository(batch),
            new FakeProductRepository(GamingProduct),
            client,
            recommendations);

        await generator.RunAsync(CancellationToken.None);

        var row = recommendations.All.Should().ContainSingle().Subject;
        row.RecommendedAction.Should().Be(RecommendedAction.MarketOpportunity);
        row.ModelVersion.Should().Be("prophet-v1");
        row.Reason.Should().Contain("trend slope");
    }

    [Fact]
    public async Task RunAsync_ForecastRow_PersistsSnapshotWithAlignedArrays()
    {
        var clock = new FakeDateTimeProvider { UtcNow = new DateTime(2026, 6, 1, 12, 0, 0, DateTimeKind.Utc) };
        var today = clock.UtcNow.Date;
        var store = GamingStore("Store A");
        var batch = new InventoryBatch(
            store.Id, GamingProduct.Id, 10, 5m, 10m, reorderPoint: 5, leadTimeDays: 2, expiryDate: null);
        var metrics = SalesHistory(store.Id, GamingProduct.Id, today, 20, 5);
        var recommendations = new FakeRecommendationRepository();
        var client = new FakeForecastingClient((series, horizon) => FlatForecast(series.Count, horizon));
        var generator = CreateGenerator(
            clock,
            new FakeStoreRepository(store),
            new FakeDailyProductMetricRepository(metrics.ToArray()),
            new FakeDailyMarketSignalRepository(),
            new FakeInventoryBatchRepository(batch),
            new FakeProductRepository(GamingProduct),
            client,
            recommendations);

        await generator.RunAsync(CancellationToken.None);

        var row = recommendations.All.Should().ContainSingle().Subject;
        row.ForecastSnapshotJson.Should().NotBeNullOrEmpty();

        var snapshot = System.Text.Json.JsonSerializer.Deserialize<global::Application.DTOs.Recommendations.ForecastSnapshot>(
            row.ForecastSnapshotJson!,
            new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web));
        snapshot.Should().NotBeNull();
        snapshot!.Dates.Should().HaveCount(194);
        snapshot.Yhat.Should().HaveCount(snapshot.Dates.Count);
        snapshot.Lower.Should().HaveCount(snapshot.Dates.Count);
        snapshot.Upper.Should().HaveCount(snapshot.Dates.Count);
        snapshot.Actuals.Should().HaveCount(180);
        snapshot.Actuals.TakeLast(20).Should().OnlyContain(v => v == 5);
    }

    [Fact]
    public async Task RunAsync_FallbackRow_HasNoSnapshot()
    {
        var clock = new FakeDateTimeProvider { UtcNow = new DateTime(2026, 6, 1, 12, 0, 0, DateTimeKind.Utc) };
        var today = clock.UtcNow.Date;
        var store = GamingStore("Store A");
        var batch = new InventoryBatch(
            store.Id, GamingProduct.Id, 10, 5m, 10m, reorderPoint: 5, leadTimeDays: 2, expiryDate: null);
        var metrics = SalesHistory(store.Id, GamingProduct.Id, today, 20, 5);
        var recommendations = new FakeRecommendationRepository();
        var client = new FakeForecastingClient((_, _) => new ForecastResponse(
            true, [], [], [], [], []));
        var generator = CreateGenerator(
            clock,
            new FakeStoreRepository(store),
            new FakeDailyProductMetricRepository(metrics.ToArray()),
            new FakeDailyMarketSignalRepository(),
            new FakeInventoryBatchRepository(batch),
            new FakeProductRepository(GamingProduct),
            client,
            recommendations);

        await generator.RunAsync(CancellationToken.None);

        var row = recommendations.All.Should().ContainSingle().Subject;
        row.ModelVersion.Should().Be("fallback-v1");
        row.ForecastSnapshotJson.Should().BeNull();
    }

    private static RecommendationGenerator CreateGenerator(
        FakeDateTimeProvider clock,
        FakeStoreRepository storeRepository,
        FakeDailyProductMetricRepository metricRepository,
        FakeDailyMarketSignalRepository signalRepository,
        FakeInventoryBatchRepository batchRepository,
        FakeProductRepository productRepository,
        FakeForecastingClient client,
        FakeRecommendationRepository recommendationRepository)
    {
        return new RecommendationGenerator(
            clock,
            storeRepository,
            metricRepository,
            signalRepository,
            batchRepository,
            productRepository,
            client,
            recommendationRepository,
            NullLogger<RecommendationGenerator>.Instance);
    }
}