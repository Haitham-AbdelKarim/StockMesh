using Application.Common.Models;
using Application.Features.Recommendations.Queries.GetRecommendationForProduct;
using Application.Features.Recommendations.Queries.GetRecommendations;
using Domain.Entities;
using Domain.Enums;
using FluentAssertions;
using StockMesh.Application.UnitTests.Fakes;

namespace StockMesh.Application.UnitTests.Features.Recommendations;

public class RecommendationQueryTests
{
    private static readonly Guid StoreId = Guid.NewGuid();
    private static readonly Guid OtherStoreId = Guid.NewGuid();

    private static readonly Product Product =
        new("PlayStation 5 Console", VerticalCategory.Gaming, "Sony");

    private static Recommendation Row(
        Guid? batchId,
        RecommendedAction action,
        DateTime generatedAt)
    {
        return new Recommendation(
            StoreId,
            Product.Id,
            batchId,
            action,
            0.5,
            false,
            null,
            0.8,
            "prophet-v1",
            "Reason.",
            generatedAt);
    }

    [Fact]
    public async Task GetRecommendations_ReturnsCallerStoreRowsPagedWithNames()
    {
        var now = DateTime.UtcNow;
        var mine = Row(null, RecommendedAction.Reorder, now);
        var mineBatch = Row(Guid.NewGuid(), RecommendedAction.Share, now.AddMinutes(-1));
        var other = new Recommendation(
            OtherStoreId, Product.Id, null, RecommendedAction.Hold, 0, false, null, 0,
            "prophet-v1", "Reason.", now);
        var handler = new GetRecommendationsQueryHandler(
            new FakeCurrentUser { StoreId = StoreId },
            new FakeRecommendationRepository(mine, mineBatch, other),
            new FakeProductRepository(Product));

        var result = await handler.Handle(
            new GetRecommendationsQuery(null, 1, 10),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.TotalCount.Should().Be(2);
        result.Value.Items.Should().HaveCount(2);
        result.Value.Items.Should().OnlyContain(r => r.ProductName == Product.Name);
    }

    [Fact]
    public async Task GetRecommendations_WithActionFilter_ReturnsSubset()
    {
        var now = DateTime.UtcNow;
        var reorder = Row(null, RecommendedAction.Reorder, now);
        var hold = Row(null, RecommendedAction.Hold, now);
        var handler = new GetRecommendationsQueryHandler(
            new FakeCurrentUser { StoreId = StoreId },
            new FakeRecommendationRepository(reorder, hold),
            new FakeProductRepository(Product));

        var result = await handler.Handle(
            new GetRecommendationsQuery(RecommendedAction.Reorder, 1, 10),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.TotalCount.Should().Be(1);
        result.Value.Items.Should().ContainSingle()
            .Which.RecommendedAction.Should().Be(RecommendedAction.Reorder);
    }

    [Fact]
    public async Task GetRecommendationForProduct_ReturnsLatestProductLevelRow()
    {
        var now = DateTime.UtcNow;
        var old = Row(null, RecommendedAction.Hold, now.AddHours(-1));
        var latest = Row(null, RecommendedAction.Reorder, now);
        var batchRow = Row(Guid.NewGuid(), RecommendedAction.Share, now.AddMinutes(1));
        var handler = new GetRecommendationForProductQueryHandler(
            new FakeCurrentUser { StoreId = StoreId },
            new FakeRecommendationRepository(old, latest, batchRow),
            new FakeProductRepository(Product));

        var result = await handler.Handle(
            new GetRecommendationForProductQuery(Product.Id),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Id.Should().Be(latest.Id);
        result.Value.RecommendedAction.Should().Be(RecommendedAction.Reorder);
        result.Value.ProductName.Should().Be(Product.Name);
    }

    [Fact]
    public async Task GetRecommendationForProduct_WithoutRows_ReturnsNotFound()
    {
        var handler = new GetRecommendationForProductQueryHandler(
            new FakeCurrentUser { StoreId = StoreId },
            new FakeRecommendationRepository(),
            new FakeProductRepository(Product));

        var result = await handler.Handle(
            new GetRecommendationForProductQuery(Product.Id),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Kind.Should().Be(FailureKind.NotFound);
    }
}