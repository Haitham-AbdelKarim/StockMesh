using Application.Features.Reservations.Commands.ResolveReservation;
using Domain.Enums;
using FluentAssertions;

namespace StockMesh.Application.UnitTests.Features.Reservations;

public class ResolveReservationCommandValidatorTests
{
    private readonly ResolveReservationCommandValidator _validator = new();

    [Theory]
    [InlineData(ReservationStatus.Accepted)]
    [InlineData(ReservationStatus.Cancelled)]
    public void Validate_WithAllowedOutcome_Passes(ReservationStatus outcome)
    {
        var result = _validator.Validate(
            new ResolveReservationCommand(Guid.NewGuid(), outcome));

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(ReservationStatus.Pending)]
    [InlineData(ReservationStatus.Success)]
    public void Validate_WithDisallowedOutcome_Fails(ReservationStatus outcome)
    {
        var result = _validator.Validate(
            new ResolveReservationCommand(Guid.NewGuid(), outcome));

        result.IsValid.Should().BeFalse();
    }
}