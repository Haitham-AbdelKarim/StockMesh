using Application.Common.Models;
using Application.Features.Stores.Queries.GetStoreProfile;
using Domain.Entities;
using Domain.Enums;
using FluentAssertions;
using StockMesh.Application.UnitTests.Fakes;

namespace StockMesh.Application.UnitTests.Features.Stores;

public class GetStoreProfileQueryHandlerTests
{
    [Fact]
    public async Task Handle_WhenStoreDoesNotExist_ReturnsNotFound()
    {
        var handler = CreateHandler(new FakeStoreRepository(), Guid.NewGuid());

        var result = await handler.Handle(new GetStoreProfileQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Kind.Should().Be(FailureKind.NotFound);
    }

    [Fact]
    public async Task Handle_WithExistingStore_ReturnsProfile()
    {
        var store = new Store("Downtown Pharmacy", VerticalCategory.Pharmacy, 30.05, 31.25, 15, true);
        var handler = CreateHandler(new FakeStoreRepository(store), store.Id);

        var result = await handler.Handle(new GetStoreProfileQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        var profile = result.Value!;
        profile.StoreId.Should().Be(store.Id);
        profile.Name.Should().Be("Downtown Pharmacy");
        profile.VerticalCategory.Should().Be(VerticalCategory.Pharmacy);
        profile.Latitude.Should().Be(30.05);
        profile.Longitude.Should().Be(31.25);
        profile.MaxSearchRadiusKm.Should().Be(15);
        profile.IsVerified.Should().BeTrue();
    }

    private static GetStoreProfileQueryHandler CreateHandler(
        FakeStoreRepository repository,
        Guid storeId)
    {
        return new GetStoreProfileQueryHandler(
            new FakeCurrentUser { StoreId = storeId },
            repository);
    }
}