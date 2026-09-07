using Application.DTOs.Dashboard;
using Application.Features.Dashboard.Queries.GetDashboardSummary;
using Domain.Entities;
using FluentAssertions;
using StockMesh.Application.UnitTests.Fakes;

namespace StockMesh.Application.UnitTests.Features.Dashboard;

public class GetDashboardSummaryQueryHandlerTests
{
    private static readonly Guid StoreId = Guid.NewGuid();
    private static readonly FakeDateTimeProvider Clock = new();

    [Fact]
    public async Task Handle_ReturnsTodayMetric()
    {
        var date = Clock.UtcNow.Date;
        var metric = new DailyStoreMetric(StoreId, date);
        metric.Update(100m, 20m, 30m, 40m, 10m, 5, 2, 1);

        var handler = new GetDashboardSummaryQueryHandler(
            new FakeCurrentUser { StoreId = StoreId },
            Clock,
            new FakeDailyStoreMetricRepository(metric));

        var result = await handler.Handle(new GetDashboardSummaryQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Should().BeEquivalentTo(new DashboardSummaryResponse(
            date,
            100m,
            20m,
            30m,
            40m,
            10m,
            100m + 20m - 30m - 40m - 10m,
            5,
            2,
            1));
    }

    [Fact]
    public async Task Handle_WhenNoMetricForDay_ReturnsZeros()
    {
        var handler = new GetDashboardSummaryQueryHandler(
            new FakeCurrentUser { StoreId = StoreId },
            Clock,
            new FakeDailyStoreMetricRepository());

        var result = await handler.Handle(new GetDashboardSummaryQuery(), CancellationToken.None);

        result.Value!.NetProfit.Should().Be(0m);
        result.Value.UnitsSold.Should().Be(0);
        result.Value.TransfersInUnits.Should().Be(0);
        result.Value.Date.Should().Be(Clock.UtcNow.Date);
    }

    [Fact]
    public async Task Handle_WithExplicitDate_ReturnsThatDay()
    {
        var date = new DateTime(2026, 5, 10, 0, 0, 0, DateTimeKind.Utc);
        var metric = new DailyStoreMetric(StoreId, date);
        var handler = new GetDashboardSummaryQueryHandler(
            new FakeCurrentUser { StoreId = StoreId },
            Clock,
            new FakeDailyStoreMetricRepository());

        var result = await handler.Handle(
            new GetDashboardSummaryQuery(Date: date),
            CancellationToken.None);

        result.Value!.Date.Should().Be(date);
    }
}