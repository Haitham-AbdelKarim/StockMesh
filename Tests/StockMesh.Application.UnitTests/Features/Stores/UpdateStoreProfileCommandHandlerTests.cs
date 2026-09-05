using Application.Common.Models;
using Application.Features.Stores.Commands.UpdateStoreProfile;
using Domain.Entities;
using Domain.Enums;
using FluentAssertions;
using StockMesh.Application.UnitTests.Fakes;

namespace StockMesh.Application.UnitTests.Features.Stores;

public class UpdateStoreProfileCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenStoreDoesNotExist_ReturnsNotFound()
    {
        var handler = CreateHandler(new FakeStoreRepository(), Guid.NewGuid());

        var result = await handler.Handle(
            new UpdateStoreProfileCommand("Renamed Store", 30.1, 31.3, 40),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Kind.Should().Be(FailureKind.NotFound);
    }

    [Fact]
    public async Task Handle_WithExistingStore_UpdatesProfileAndPersists()
    {
        var store = new Store("Original Name", VerticalCategory.Gaming, 30.05, 31.25, 30);
        var handler = CreateHandler(new FakeStoreRepository(store), store.Id);

        var result = await handler.Handle(
            new UpdateStoreProfileCommand("Renamed Store", 30.5, 31.6, 45),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        store.Name.Should().Be("Renamed Store");
        store.Latitude.Should().Be(30.5);
        store.Longitude.Should().Be(31.6);
        store.MaxSearchRadiusKm.Should().Be(45);
        store.VerticalCategory.Should().Be(VerticalCategory.Gaming);

        result.Value.Should().NotBeNull();
        var profile = result.Value!;
        profile.StoreId.Should().Be(store.Id);
        profile.Name.Should().Be("Renamed Store");
        profile.VerticalCategory.Should().Be(VerticalCategory.Gaming);
        profile.Latitude.Should().Be(30.5);
        profile.MaxSearchRadiusKm.Should().Be(45);
        profile.IsVerified.Should().BeFalse();
    }

    private static UpdateStoreProfileCommandHandler CreateHandler(
        FakeStoreRepository repository,
        Guid storeId)
    {
        return new UpdateStoreProfileCommandHandler(
            new FakeCurrentUser { StoreId = storeId },
            repository);
    }
}