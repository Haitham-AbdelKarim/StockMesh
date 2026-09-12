using Domain.Enums;
using FluentValidation;

namespace Application.Features.Reservations.Commands.ResolveReservation;

public sealed class ResolveReservationCommandValidator : AbstractValidator<ResolveReservationCommand>
{
    public ResolveReservationCommandValidator()
    {
        RuleFor(x => x.ReservationId)
            .NotEmpty()
            .WithMessage("Reservation is required.");

        RuleFor(x => x.Outcome)
            .Must(o => o == ReservationStatus.Accepted || o == ReservationStatus.Cancelled)
            .WithMessage("Resolution outcome must be either Accepted or Cancelled.");
    }
}