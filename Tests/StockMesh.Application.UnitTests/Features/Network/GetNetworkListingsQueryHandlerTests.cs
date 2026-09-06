using Application.Common.Models;
using Application.Features.Network.Queries.GetNetworkListings;
using Domain.Entities;
using Domain.Enums;
using FluentAssertions;
using StockMesh.Application.UnitTests.Fakes;

namespace StockMesh.Application.UnitTests.Features.Network;

public class GetNetworkListingsQueryHandlerTests
{
    private static readonly Store CurrentStore =
        new("My Store", VerticalCategory.Pharmacy, 30.0444, 31.2357, 50);

    private static readonly Store NearbyPeer =
        new("Nearby Pharmacy", VerticalCategory.Pharmacy, 30.0131, 31.2089, 30);

    private static readonly Store SameCoordinates =
        new("Identical Coords", VerticalCategory.Pharmacy, 30.0444, 31.2357, 30);

    private static readonly Store FarAway =
        new("Alexandria Pharmacy", VerticalCategory.Pharmacy, 31.2001, 29.9187, 30);

    private static readonly Store ForeignVertical =
        new("Gaming Near Me", VerticalCategory.Gaming, 30.0444, 31.2357, 30);

    private static readonly Product Paracetamol =
        new("Paracetamol 500mg", VerticalCategory.Pharmacy, "Panadol");

    private static readonly Product VitaminC =
        new("Vitamin C 1000mg", VerticalCategory.Pharmacy);

    private static readonly Product Playstation =
        new("PlayStation 5 Console", VerticalCategory.Gaming, "Sony");

    [Fact]
    public async Task Handle_ReturnsNearbySharedBatchesWithNamesAndDistance()
    {
        var shared = new InventoryBatch(NearbyPeer.Id, Paracetamol.Id, 10, 5m, 12m);
        shared.MarkAsShared(3);
        var handler = CreateHandler(
            CurrentStore,
            new FakeStoreRepository(CurrentStore, NearbyPeer),
            new FakeInventoryBatchRepository(shared),
            new FakeProductRepository(Paracetamol));

        var result = await handler.Handle(new GetNetworkListingsQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var listing = result.Value!.Items.Should().ContainSingle().Subject;
        listing.BatchId.Should().Be(shared.Id);
        listing.StoreId.Should().Be(NearbyPeer.Id);
        listing.StoreName.Should().Be(NearbyPeer.Name);
        listing.ProductId.Should().Be(Paracetamol.Id);
        listing.ProductName.Should().Be(Paracetamol.Name);
        listing.SharedQuantity.Should().Be(3);
        listing.ExpiryDate.Should().BeNull();
        listing.DistanceKm.Should().BeGreaterThan(0);
        listing.DistanceKm.Should().BeLessThan(50);
    }

    [Fact]
    public async Task Handle_ExcludesOwnStoreListings()
    {
        var ownShared = new InventoryBatch(CurrentStore.Id, Paracetamol.Id, 10, 5m, 12m);
        ownShared.MarkAsShared(3);
        var handler = CreateHandler(
            CurrentStore,
            new FakeStoreRepository(CurrentStore, NearbyPeer),
            new FakeInventoryBatchRepository(ownShared),
            new FakeProductRepository(Paracetamol));

        var result = await handler.Handle(new GetNetworkListingsQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ExcludesDifferentVerticalEvenAtZeroDistance()
    {
        var gamingShared = new InventoryBatch(ForeignVertical.Id, Playstation.Id, 5, 100m, 150m);
        gamingShared.MarkAsShared(2);
        var handler = CreateHandler(
            CurrentStore,
            new FakeStoreRepository(CurrentStore, NearbyPeer, ForeignVertical),
            new FakeInventoryBatchRepository(gamingShared),
            new FakeProductRepository(Paracetamol, Playstation));

        var result = await handler.Handle(new GetNetworkListingsQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_UsesStoresMaxSearchRadiusAsDefault()
    {
        var farShared = new InventoryBatch(FarAway.Id, Paracetamol.Id, 10, 5m, 12m);
        farShared.MarkAsShared(4);
        var handler = CreateHandler(
            CurrentStore,
            new FakeStoreRepository(CurrentStore, NearbyPeer, FarAway),
            new FakeInventoryBatchRepository(farShared),
            new FakeProductRepository(Paracetamol));

        var result = await handler.Handle(new GetNetworkListingsQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ExplicitMaxDistanceKm_BringsInDistantStore()
    {
        var farShared = new InventoryBatch(FarAway.Id, Paracetamol.Id, 10, 5m, 12m);
        farShared.MarkAsShared(4);
        var handler = CreateHandler(
            CurrentStore,
            new FakeStoreRepository(CurrentStore, FarAway),
            new FakeInventoryBatchRepository(farShared),
            new FakeProductRepository(Paracetamol));

        var result = await handler.Handle(
            new GetNetworkListingsQuery(MaxDistanceKm: 300),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var listing = result.Value!.Items.Should().ContainSingle().Subject;
        listing.StoreId.Should().Be(FarAway.Id);
        listing.SharedQuantity.Should().Be(4);
    }

    [Fact]
    public async Task Handle_SearchFiltersByProductName()
    {
        var para = new InventoryBatch(NearbyPeer.Id, Paracetamol.Id, 10, 5m, 12m);
        para.MarkAsShared(2);
        var vitamin = new InventoryBatch(NearbyPeer.Id, VitaminC.Id, 10, 3m, 6m);
        vitamin.MarkAsShared(2);
        var handler = CreateHandler(
            CurrentStore,
            new FakeStoreRepository(CurrentStore, NearbyPeer),
            new FakeInventoryBatchRepository(para, vitamin),
            new FakeProductRepository(Paracetamol, VitaminC));

        var result = await handler.Handle(
            new GetNetworkListingsQuery(Search: "vitamin"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var listing = result.Value!.Items.Should().ContainSingle().Subject;
        listing.ProductId.Should().Be(VitaminC.Id);
    }

    [Fact]
    public async Task Handle_SearchMatchesBrand()
    {
        var para = new InventoryBatch(NearbyPeer.Id, Paracetamol.Id, 10, 5m, 12m);
        para.MarkAsShared(2);
        var vitamin = new InventoryBatch(NearbyPeer.Id, VitaminC.Id, 10, 3m, 6m);
        vitamin.MarkAsShared(2);
        var handler = CreateHandler(
            CurrentStore,
            new FakeStoreRepository(CurrentStore, NearbyPeer),
            new FakeInventoryBatchRepository(para, vitamin),
            new FakeProductRepository(Paracetamol, VitaminC));

        var result = await handler.Handle(
            new GetNetworkListingsQuery(Search: "panadol"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().ContainSingle(x => x.ProductId == Paracetamol.Id);
    }

    [Fact]
    public async Task Handle_OrdersByDistanceThenExpiryDate()
    {
        var soon = new DateTime(2026, 12, 1);
        var later = new DateTime(2027, 6, 1);
        var nearExpiry = new DateTime(2026, 11, 1);

        var sameSoon = new InventoryBatch(
            SameCoordinates.Id, Paracetamol.Id, 10, 5m, 12m, expiryDate: soon);
        sameSoon.MarkAsShared(2);
        var sameLater = new InventoryBatch(
            SameCoordinates.Id, Paracetamol.Id, 10, 5m, 12m, expiryDate: later);
        sameLater.MarkAsShared(2);
        var nearAtDistance = new InventoryBatch(
            NearbyPeer.Id, Paracetamol.Id, 10, 5m, 12m, expiryDate: nearExpiry);
        nearAtDistance.MarkAsShared(2);

        var handler = CreateHandler(
            CurrentStore,
            new FakeStoreRepository(CurrentStore, SameCoordinates, NearbyPeer),
            new FakeInventoryBatchRepository(sameSoon, sameLater, nearAtDistance),
            new FakeProductRepository(Paracetamol));

        var result = await handler.Handle(new GetNetworkListingsQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Select(x => x.BatchId)
            .Should().ContainInOrder(sameSoon.Id, sameLater.Id, nearAtDistance.Id);
    }

    [Fact]
    public async Task Handle_WithPaging_ReturnsRequestedSlice()
    {
        var batch1 = new InventoryBatch(NearbyPeer.Id, Paracetamol.Id, 10, 5m, 12m);
        batch1.MarkAsShared(1);
        var batch2 = new InventoryBatch(NearbyPeer.Id, Paracetamol.Id, 10, 5m, 12m);
        batch2.MarkAsShared(1);
        var batch3 = new InventoryBatch(NearbyPeer.Id, Paracetamol.Id, 10, 5m, 12m);
        batch3.MarkAsShared(1);
        var handler = CreateHandler(
            CurrentStore,
            new FakeStoreRepository(CurrentStore, NearbyPeer),
            new FakeInventoryBatchRepository(batch1, batch2, batch3),
            new FakeProductRepository(Paracetamol));

        var result = await handler.Handle(
            new GetNetworkListingsQuery(Page: 2, PageSize: 2),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.TotalCount.Should().Be(3);
        result.Value.Items.Should().HaveCount(1);
        result.Value.Page.Should().Be(2);
        result.Value.TotalPages.Should().Be(2);
    }

    [Fact]
    public async Task Handle_WhenCurrentStoreMissing_ReturnsNotFound()
    {
        var handler = CreateHandler(
            CurrentStore,
            new FakeStoreRepository(),
            new FakeInventoryBatchRepository(),
            new FakeProductRepository(Paracetamol));

        var result = await handler.Handle(new GetNetworkListingsQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Kind.Should().Be(FailureKind.NotFound);
    }

    private static GetNetworkListingsQueryHandler CreateHandler(
        Store currentStore,
        FakeStoreRepository storeRepository,
        FakeInventoryBatchRepository inventoryBatchRepository,
        FakeProductRepository productRepository)
    {
        return new GetNetworkListingsQueryHandler(
            new FakeCurrentUser { StoreId = currentStore.Id },
            storeRepository,
            inventoryBatchRepository,
            productRepository);
    }
}