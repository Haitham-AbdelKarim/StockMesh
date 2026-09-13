using Domain.Entities;
using Domain.Enums;
using Domain.Exceptions;
using FluentAssertions;

namespace StockMesh.Domain.UnitTests.Entities;

public class StockReservationTests
{
    [Fact]
    public void Resolve_pending_to_accepted_sets_status_and_timestamp()
    {
        var reservation = CreateReservation();

        reservation.Resolve(ReservationStatus.Accepted);

        reservation.Status.Should().Be(ReservationStatus.Accepted);
        reservation.ResolvedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void Resolve_pending_to_cancelled_sets_status_and_timestamp()
    {
        var reservation = CreateReservation();

        reservation.Resolve(ReservationStatus.Cancelled);

        reservation.Status.Should().Be(ReservationStatus.Cancelled);
        reservation.ResolvedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void Resolve_accepted_to_success_sets_status()
    {
        var reservation = CreateReservation();
        reservation.Resolve(ReservationStatus.Accepted);

        reservation.Resolve(ReservationStatus.Success);

        reservation.Status.Should().Be(ReservationStatus.Success);
    }

    [Fact]
    public void Resolve_accepted_to_cancelled_sets_status()
    {
        var reservation = CreateReservation();
        reservation.Resolve(ReservationStatus.Accepted);

        reservation.Resolve(ReservationStatus.Cancelled);

        reservation.Status.Should().Be(ReservationStatus.Cancelled);
    }

    [Fact]
    public void Resolve_pending_to_success_throws_InvalidReservationTransitionException()
    {
        var reservation = CreateReservation();

        var act = () => reservation.Resolve(ReservationStatus.Success);

        act.Should().Throw<InvalidReservationTransitionException>();
        reservation.Status.Should().Be(ReservationStatus.Pending);
    }

    [Fact]
    public void Resolve_success_is_terminal()
    {
        var reservation = CreateReservation();
        reservation.Resolve(ReservationStatus.Accepted);
        reservation.Resolve(ReservationStatus.Success);

        var act = () => reservation.Resolve(ReservationStatus.Cancelled);

        act.Should().Throw<InvalidReservationTransitionException>();
        reservation.Status.Should().Be(ReservationStatus.Success);
    }

    [Fact]
    public void Resolve_cancelled_is_terminal()
    {
        var reservation = CreateReservation();
        reservation.Resolve(ReservationStatus.Cancelled);

        var act = () => reservation.Resolve(ReservationStatus.Accepted);

        act.Should().Throw<InvalidReservationTransitionException>();
        reservation.Status.Should().Be(ReservationStatus.Cancelled);
    }

    [Fact]
    public void Resolve_accepted_to_accepted_throws_InvalidReservationTransitionException()
    {
        var reservation = CreateReservation();
        reservation.Resolve(ReservationStatus.Accepted);

        var act = () => reservation.Resolve(ReservationStatus.Accepted);

        act.Should().Throw<InvalidReservationTransitionException>();
    }

    private static StockReservation CreateReservation()
    {
        return new StockReservation(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            5,
            10m);
    }
}