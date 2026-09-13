using Application.Features.Dashboard.Queries.GetTopSellers;
using Domain.Entities;
using Domain.Enums;
using FluentAssertions;
using StockMesh.Application.UnitTests.Fakes;

namespace StockMesh.Application.UnitTests.Features.Dashboard;

public class GetTopSellersQueryHandlerTests
{
    private static readonly Guid StoreId = Guid.NewGuid();
    private static readonly FakeDateTimeProvider Clock = new();
    private static readonly Product Product1 = new("Paracetamol", VerticalCategory.Pharmacy, "Panadol");
    private static readonly Product Product2 = new("Ibuprofen", VerticalCategory.Pharmacy, "Advil");

    [Fact]
    public async Task Handle_AggregatesByProductAndOrdersByUnits()
    {
        var metrics = new FakeDailyProductMetricRepository(
            CreateProductMetric(Product1.Id, 3, 30m, Clock.UtcNow.Date.AddDays(-2)),
            CreateProductMetric(Product1.Id, 5, 50m, Clock.UtcNow.Date.AddDays(-1)),
            CreateProductMetric(Product2.Id, 10, 90m, Clock.UtcNow.Date.AddDays(-1)));

        var handler = new GetTopSellersQueryHandler(
            new FakeCurrentUser { StoreId = StoreId },
            Clock,
            metrics,
            new FakeProductRepository(Product1, Product2));

        var result = await handler.Handle(
            new GetTopSellersQuery(TopN: 10),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value[0].ProductName.Should().Be(Product2.Name);
        result.Value[0].UnitsSold.Should().Be(10);
        result.Value[1].ProductName.Should().Be(Product1.Name);
        result.Value[1].UnitsSold.Should().Be(8);
        result.Value[1].SalesRevenue.Should().Be(80m);
    }

    [Fact]
    public async Task Handle_RespectsTopN()
    {
        var metrics = new FakeDailyProductMetricRepository(
            CreateProductMetric(Product1.Id, 5, 50m, Clock.UtcNow.Date.AddDays(-1)),
            CreateProductMetric(Product2.Id, 2, 20m, Clock.UtcNow.Date.AddDays(-1)));

        var handler = new GetTopSellersQueryHandler(
            new FakeCurrentUser { StoreId = StoreId },
            Clock,
            metrics,
            new FakeProductRepository(Product1, Product2));

        var result = await handler.Handle(
            new GetTopSellersQuery(TopN: 1),
            CancellationToken.None);

        result.Value.Should().ContainSingle().Which.ProductName.Should().Be(Product1.Name);
    }

    [Fact]
    public async Task Handle_WhenNoSales_ReturnsEmpty()
    {
        var handler = new GetTopSellersQueryHandler(
            new FakeCurrentUser { StoreId = StoreId },
            Clock,
            new FakeDailyProductMetricRepository(),
            new FakeProductRepository());

        var result = await handler.Handle(
            new GetTopSellersQuery(),
            CancellationToken.None);

        result.Value.Should().BeEmpty();
    }

    [Fact]
    public void Validate_WithOutOfRangeTopN_ReturnsErrors()
    {
        var validator = new GetTopSellersQueryValidator();

        var result = validator.Validate(new GetTopSellersQuery(TopN: 0));

        result.IsValid.Should().BeFalse();
    }

    private static DailyProductMetric CreateProductMetric(Guid productId, int units, decimal revenue, DateTime date)
    {
        var metric = new DailyProductMetric(StoreId, productId, date);
        metric.Update(units, revenue, 0m, 0, 0m);

        return metric;
    }
}