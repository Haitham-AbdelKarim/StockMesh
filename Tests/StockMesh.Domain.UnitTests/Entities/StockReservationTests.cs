using Domain.Entities;
using Domain.Enums;
using Domain.Exceptions;
using FluentAssertions;

namespace StockMesh.Domain.UnitTests.Entities;

public class StockReservationTests
{
    [Fact]
    public void Resolve_pending_to_success_sets_status_and_timestamp()
    {
        var reservation = new StockReservation(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            5,
            10m);

        reservation.Resolve(ReservationStatus.Success);

        reservation.Status.Should().Be(ReservationStatus.Success);
        reservation.ResolvedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void Resolve_pending_to_cancelled_sets_status_and_timestamp()
    {
        var reservation = new StockReservation(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            5,
            10m);

        reservation.Resolve(ReservationStatus.Cancelled);

        reservation.Status.Should().Be(ReservationStatus.Cancelled);
        reservation.ResolvedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void Resolve_non_pending_throws_InvalidReservationTransitionException()
    {
        var reservation = new StockReservation(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            5,
            10m);

        reservation.Resolve(ReservationStatus.Success);

        var act = () => reservation.Resolve(ReservationStatus.Cancelled);

        act.Should().Throw<InvalidReservationTransitionException>();
    }
}