using Application.Features.Reservations.Queries.GetReservations;
using Domain.Entities;
using Domain.Enums;
using FluentAssertions;
using StockMesh.Application.UnitTests.Fakes;

namespace StockMesh.Application.UnitTests.Features.Reservations;

public class GetReservationsQueryHandlerTests
{
    private static readonly Store RequestingStore = new(
        "Central Depot",
        VerticalCategory.Grocery,
        31.2,
        29.9,
        50);

    private static readonly Store OwningStore = new(
        "North Warehouse",
        VerticalCategory.Grocery,
        31.25,
        29.95,
        50);

    private static readonly Product Product =
        new("Basmati Rice 5kg", VerticalCategory.Grocery, "AlArz");

    private static readonly Guid MyStoreId = OwningStore.Id;

    [Fact]
    public async Task Handle_ReturnsPaginatedReservationsWithEnrichedNames()
    {
        var batch = new InventoryBatch(MyStoreId, Product.Id, 20, 10m, 15m);
        var reservations = new[]
        {
            CreateReservation(batch.Id, incoming: true, ReservationStatus.Pending),
            CreateReservation(batch.Id, incoming: false, ReservationStatus.Success),
            CreateReservation(batch.Id, incoming: true, ReservationStatus.Cancelled)
        };
        var handler = CreateHandler(reservations, batch);

        var result = await handler.Handle(
            new GetReservationsQuery(),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().HaveCount(3);
        result.Value.TotalCount.Should().Be(3);
        result.Value.Items.Should().OnlyContain(i =>
            i.ProductId == Product.Id
            && i.ProductName == Product.Name
            && !string.IsNullOrEmpty(i.RequestingStoreName)
            && !string.IsNullOrEmpty(i.OwningStoreName));
    }

    [Fact]
    public async Task Handle_WithIncomingFilter_ReturnsOnlyReservationsRequestingMyStock()
    {
        var batch = new InventoryBatch(MyStoreId, Product.Id, 20, 10m, 15m);
        var reservations = new[]
        {
            CreateReservation(batch.Id, incoming: true, ReservationStatus.Pending),
            CreateReservation(batch.Id, incoming: false, ReservationStatus.Pending)
        };
        var handler = CreateHandler(reservations, batch);

        var result = await handler.Handle(
            new GetReservationsQuery(Incoming: true),
            CancellationToken.None);

        result.Value!.Items.Should().ContainSingle();
        result.Value.Items.Single().RequestingStoreId.Should().Be(RequestingStore.Id);
        result.Value.Items.Single().OwningStoreId.Should().Be(MyStoreId);
    }

    [Fact]
    public async Task Handle_WithStatusFilter_ReturnsOnlyMatchingStatus()
    {
        var batch = new InventoryBatch(MyStoreId, Product.Id, 20, 10m, 15m);
        var reservations = new[]
        {
            CreateReservation(batch.Id, incoming: true, ReservationStatus.Pending),
            CreateReservation(batch.Id, incoming: false, ReservationStatus.Success)
        };
        var handler = CreateHandler(reservations, batch);

        var result = await handler.Handle(
            new GetReservationsQuery(Status: ReservationStatus.Pending),
            CancellationToken.None);

        result.Value!.Items.Should().ContainSingle()
            .Which.Status.Should().Be(ReservationStatus.Pending);
    }

    [Fact]
    public async Task Handle_WhenNoReservations_ReturnsEmptyPage()
    {
        var batch = new InventoryBatch(MyStoreId, Product.Id, 20, 10m, 15m);
        var handler = CreateHandler([], batch);

        var result = await handler.Handle(
            new GetReservationsQuery(),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().BeEmpty();
        result.Value.TotalCount.Should().Be(0);
    }

    [Fact]
    public void Validate_WithOutOfRangePaging_ReturnsErrors()
    {
        var validator = new GetReservationsQueryValidator();

        var result = validator.Validate(new GetReservationsQuery(Page: 0, PageSize: 0));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().NotBeEmpty();
    }

    [Fact]
    public void Validate_WithDefaults_IsValid()
    {
        var validator = new GetReservationsQueryValidator();

        var result = validator.Validate(new GetReservationsQuery());

        result.IsValid.Should().BeTrue();
    }

    private static StockReservation CreateReservation(
        Guid batchId,
        bool incoming,
        ReservationStatus status)
    {
        var reservation = incoming
            ? new StockReservation(
                batchId,
                RequestingStore.Id,
                MyStoreId,
                5,
                15m)
            : new StockReservation(
                batchId,
                MyStoreId,
                RequestingStore.Id,
                5,
                15m);

        if (status != ReservationStatus.Pending)
        {
            reservation.Resolve(status);
        }

        return reservation;
    }

    private static GetReservationsQueryHandler CreateHandler(
        IReadOnlyList<StockReservation> reservations,
        InventoryBatch batch)
    {
        return new GetReservationsQueryHandler(
            new FakeCurrentUser { StoreId = MyStoreId },
            new FakeStockReservationRepository(reservations.ToArray()),
            new FakeInventoryBatchRepository(batch),
            new FakeProductRepository(Product),
            new FakeStoreRepository(RequestingStore, OwningStore));
    }
}