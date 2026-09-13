using Application.Features.Dashboard.Queries.GetSlowMovers;
using Domain.Entities;
using Domain.Enums;
using FluentAssertions;
using StockMesh.Application.UnitTests.Fakes;

namespace StockMesh.Application.UnitTests.Features.Dashboard;

public class GetSlowMoversQueryHandlerTests
{
    private static readonly Guid StoreId = Guid.NewGuid();
    private static readonly FakeDateTimeProvider Clock = new();
    private static readonly Product Product = new("Paracetamol", VerticalCategory.Pharmacy, "Panadol");

    [Fact]
    public async Task Handle_ComputesDaysOfCoverFromOnHandAndRecentSales()
    {
        var batch = new InventoryBatch(StoreId, Product.Id, 90, 5m, 12.5m);
        var metrics = new FakeDailyProductMetricRepository(
            CreateProductMetric(Product.Id, 30, 300m, Clock.UtcNow.Date.AddDays(-2)));

        var handler = new GetSlowMoversQueryHandler(
            new FakeCurrentUser { StoreId = StoreId },
            Clock,
            metrics,
            new FakeInventoryBatchRepository(batch),
            new FakeProductRepository(Product));

        var result = await handler.Handle(
            new GetSlowMoversQuery(Days: 30),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle();
        result.Value.Single().QuantityRemaining.Should().Be(90);
        result.Value.Single().UnitsSoldLastDays.Should().Be(30);
        result.Value.Single().EstimatedDaysOfCover.Should().Be(90);
    }

    [Fact]
    public async Task Handle_WhenNoSales_ReturnsEmpty()
    {
        var handler = new GetSlowMoversQueryHandler(
            new FakeCurrentUser { StoreId = StoreId },
            Clock,
            new FakeDailyProductMetricRepository(),
            new FakeInventoryBatchRepository(),
            new FakeProductRepository());

        var result = await handler.Handle(
            new GetSlowMoversQuery(),
            CancellationToken.None);

        result.Value.Should().BeEmpty();
    }

    [Fact]
    public void Validate_WithOutOfRangeDays_ReturnsErrors()
    {
        var validator = new GetSlowMoversQueryValidator();

        var result = validator.Validate(new GetSlowMoversQuery(Days: 6));

        result.IsValid.Should().BeFalse();
    }

    private static DailyProductMetric CreateProductMetric(Guid productId, int units, decimal revenue, DateTime date)
    {
        var metric = new DailyProductMetric(StoreId, productId, date);
        metric.Update(units, revenue, 0m, 0, 0m);

        return metric;
    }
}