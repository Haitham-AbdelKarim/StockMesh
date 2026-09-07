using Application.Features.Dashboard.Queries.GetSalesTrend;
using Domain.Entities;
using FluentAssertions;
using StockMesh.Application.UnitTests.Fakes;

namespace StockMesh.Application.UnitTests.Features.Dashboard;

public class GetSalesTrendQueryHandlerTests
{
    private static readonly Guid StoreId = Guid.NewGuid();
    private static readonly FakeDateTimeProvider Clock = new();

    [Fact]
    public async Task Handle_ReturnsDayPointsZeroFilled()
    {
        var days = 3;
        var metrics = new FakeDailyStoreMetricRepository(
            new DailyStoreMetric(StoreId, Clock.UtcNow.Date),
            new DailyStoreMetric(StoreId, Clock.UtcNow.Date.AddDays(-1)),
            new DailyStoreMetric(StoreId, Clock.UtcNow.Date.AddDays(-2)));

        var handler = new GetSalesTrendQueryHandler(
            new FakeCurrentUser { StoreId = StoreId },
            Clock,
            metrics);

        var result = await handler.Handle(
            new GetSalesTrendQuery(Days: days),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Should().HaveCount(days);
        result.Value.Should().OnlyContain(p => p.UnitsSold == 0 && p.SalesRevenue == 0m);
    }

    [Fact]
    public async Task Handle_WithMetricData_ReturnsStoredValues()
    {
        var today = Clock.UtcNow.Date;
        var metric = new DailyStoreMetric(StoreId, today);
        metric.Update(250m, 0m, 50m, 0m, 0m, 10, 0, 0);

        var handler = new GetSalesTrendQueryHandler(
            new FakeCurrentUser { StoreId = StoreId },
            Clock,
            new FakeDailyStoreMetricRepository(metric));

        var result = await handler.Handle(
            new GetSalesTrendQuery(Days: 1),
            CancellationToken.None);

        result.Value!.Should().ContainSingle().Which.SalesRevenue.Should().Be(250m);
        result.Value.Single().UnitsSold.Should().Be(10);
    }

    [Fact]
    public void Validate_WithOutOfRangeDays_ReturnsErrors()
    {
        var validator = new GetSalesTrendQueryValidator();

        var result = validator.Validate(new GetSalesTrendQuery(Days: 0));

        result.IsValid.Should().BeFalse();
    }
}