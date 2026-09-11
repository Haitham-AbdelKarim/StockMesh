using Application.DTOs.Recommendations;
using Application.Features.Recommendations.Queries.GetMarketHistory;
using Domain.Entities;
using Domain.Enums;
using FluentAssertions;
using StockMesh.Application.UnitTests.Fakes;

namespace StockMesh.Application.UnitTests.Features.Recommendations;

public class MarketHistoryTests
{
    private static readonly Guid StoreId = Guid.NewGuid();

    private static readonly Product GamingProduct =
        new("PlayStation 5 Console", VerticalCategory.Gaming, "Sony");

    private static DailyMarketSignal Signal(DateTime date, int reservations, int volume, int stores)
    {
        var signal = new DailyMarketSignal(GamingProduct.Id, VerticalCategory.Gaming, date);
        signal.Update(reservations, volume, stores);

        return signal;
    }

    [Fact]
    public async Task Handle_ReturnsCallerVerticalHistoryOrderedByDate()
    {
        var today = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);
        var repo = new FakeDailyMarketSignalRepository(
            Signal(today.AddDays(-2), 1, 10, 2),
            Signal(today.AddDays(-1), 3, 30, 4));
        var handler = new GetMarketHistoryQueryHandler(
            new FakeCurrentUser { StoreId = StoreId, VerticalCategory = VerticalCategory.Gaming },
            new FakeDateTimeProvider { UtcNow = today.AddHours(12) },
            repo);

        var result = await handler.Handle(
            new GetMarketHistoryQuery(GamingProduct.Id, 7),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value![0].Date.Should().Be(today.AddDays(-2));
        result.Value[0].TransferVolume.Should().Be(10);
        result.Value[1].ReservationCount.Should().Be(3);
        result.Value[1].ParticipatingStoreCount.Should().Be(4);
    }

    [Fact]
    public async Task Handle_ExcludesOtherVerticals()
    {
        var today = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);
        var other = new DailyMarketSignal(GamingProduct.Id, VerticalCategory.Pharmacy, today.AddDays(-1));
        other.Update(5, 50, 6);
        var repo = new FakeDailyMarketSignalRepository(other);
        var handler = new GetMarketHistoryQueryHandler(
            new FakeCurrentUser { StoreId = StoreId, VerticalCategory = VerticalCategory.Gaming },
            new FakeDateTimeProvider { UtcNow = today.AddHours(12) },
            repo);

        var result = await handler.Handle(
            new GetMarketHistoryQuery(GamingProduct.Id, 7),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_EmptyHistory_ReturnsEmpty()
    {
        var handler = new GetMarketHistoryQueryHandler(
            new FakeCurrentUser { StoreId = StoreId, VerticalCategory = VerticalCategory.Gaming },
            new FakeDateTimeProvider { UtcNow = DateTime.UtcNow },
            new FakeDailyMarketSignalRepository());

        var result = await handler.Handle(
            new GetMarketHistoryQuery(Guid.NewGuid(), 7),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public void Validate_RejectsEmptyProductAndOutOfRangeDays()
    {
        var validator = new GetMarketHistoryQueryValidator();

        validator.Validate(new GetMarketHistoryQuery(Guid.Empty, 7)).IsValid.Should().BeFalse();
        validator.Validate(new GetMarketHistoryQuery(Guid.NewGuid(), 0)).IsValid.Should().BeFalse();
        validator.Validate(new GetMarketHistoryQuery(Guid.NewGuid(), 366)).IsValid.Should().BeFalse();
        validator.Validate(new GetMarketHistoryQuery(Guid.NewGuid(), 90)).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Mapper_CorruptSnapshotJson_MapsNullForecast()
    {
        var row = new Recommendation(
            StoreId,
            GamingProduct.Id,
            null,
            RecommendedAction.Reorder,
            0.5,
            false,
            null,
            0.8,
            "prophet-v1",
            "Reason.",
            DateTime.UtcNow);
        row.SetForecastSnapshot("not-json{{{");

        var response = RecommendationMapper.ToResponse(row, GamingProduct.Name);

        response.Forecast.Should().BeNull();
        response.RecommendedAction.Should().Be(RecommendedAction.Reorder);
    }

    [Fact]
    public void Mapper_ValidSnapshotJson_MapsForecast()
    {
        var row = new Recommendation(
            StoreId,
            GamingProduct.Id,
            null,
            RecommendedAction.Reorder,
            0.5,
            false,
            null,
            0.8,
            "prophet-v1",
            "Reason.",
            DateTime.UtcNow);
        row.SetForecastSnapshot(RecommendationMapper.SerializeSnapshot(new ForecastSnapshot(
            ["2026-06-01", "2026-06-02"],
            [3, 4],
            [3.5, 4.5],
            [3.0, 4.0],
            [4.0, 5.0])));

        var response = RecommendationMapper.ToResponse(row, GamingProduct.Name);

        response.Forecast.Should().NotBeNull();
        response.Forecast!.Dates.Should().Equal("2026-06-01", "2026-06-02");
        response.Forecast.Actuals.Should().Equal(3, 4);
        response.Forecast.Yhat.Should().Equal(3.5, 4.5);
    }
}