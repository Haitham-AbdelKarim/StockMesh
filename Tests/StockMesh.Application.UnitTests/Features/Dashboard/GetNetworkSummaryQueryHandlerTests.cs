using Application.Features.Dashboard.Queries.GetNetworkSummary;
using Domain.Entities;
using Domain.Enums;
using FluentAssertions;
using StockMesh.Application.UnitTests.Fakes;

namespace StockMesh.Application.UnitTests.Features.Dashboard;

public class GetNetworkSummaryQueryHandlerTests
{
    private static readonly Guid StoreId = Guid.NewGuid();

    [Fact]
    public async Task Handle_AggregatesTransfersAndReservationHitRate()
    {
        var batchId = Guid.NewGuid();
        var reservation1 = CreateReservation(ReservationStatus.Success);
        var reservation2 = CreateReservation(ReservationStatus.Cancelled);
        var clock = new FakeDateTimeProvider { UtcNow = reservation1.CreatedAt };

        var movements = new FakeStockMovementRepository(
            new StockMovement(StoreId, batchId, MovementType.NetworkTransferOut, 3, reservation1.CreatedAt, relatedStoreId: Guid.NewGuid(), unitPrice: 10m),
            new StockMovement(StoreId, batchId, MovementType.NetworkTransferOut, 2, reservation1.CreatedAt, relatedStoreId: Guid.NewGuid(), unitPrice: 12m),
            new StockMovement(StoreId, batchId, MovementType.NetworkTransferIn, 5, reservation1.CreatedAt, relatedStoreId: Guid.NewGuid(), unitPrice: 9m));

        var handler = new GetNetworkSummaryQueryHandler(
            new FakeCurrentUser { StoreId = StoreId },
            clock,
            movements,
            new FakeStockReservationRepository(reservation1, reservation2));

        var result = await handler.Handle(
            new GetNetworkSummaryQuery(Days: 30),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.TransfersOutCount.Should().Be(2);
        result.Value.TransfersInCount.Should().Be(1);
        result.Value.TransfersOutUnits.Should().Be(5);
        result.Value.TransfersInUnits.Should().Be(5);
        result.Value.TransfersOutRevenue.Should().Be(3m * 10m + 2m * 12m);
        result.Value.TransfersInValue.Should().Be(45m);
        result.Value.ReservationSuccessRate.Should().Be(0.5m);
    }

    [Fact]
    public async Task Handle_WhenNoResolutions_ReturnsZeroRate()
    {
        var reservation = CreateReservation(ReservationStatus.Pending);
        var clock = new FakeDateTimeProvider { UtcNow = reservation.CreatedAt };

        var handler = new GetNetworkSummaryQueryHandler(
            new FakeCurrentUser { StoreId = StoreId },
            clock,
            new FakeStockMovementRepository(),
            new FakeStockReservationRepository(reservation));

        var result = await handler.Handle(new GetNetworkSummaryQuery(), CancellationToken.None);

        result.Value!.ReservationSuccessRate.Should().Be(0m);
        result.Value.TransfersOutCount.Should().Be(0);
    }

    [Fact]
    public void Validate_WithOutOfRangeDays_ReturnsErrors()
    {
        var validator = new GetNetworkSummaryQueryValidator();

        var result = validator.Validate(new GetNetworkSummaryQuery(Days: 0));

        result.IsValid.Should().BeFalse();
    }

    private static StockReservation CreateReservation(ReservationStatus status)
    {
        var reservation = new StockReservation(
            Guid.NewGuid(),
            StoreId,
            Guid.NewGuid(),
            2,
            10m,
            holdExpiresAt: DateTime.UtcNow.AddMinutes(15));

        if (status == ReservationStatus.Pending)
        {
            return reservation;
        }

        reservation.Resolve(status);

        return reservation;
    }
}