using FluentValidation;

namespace Application.Features.Reservations.Commands.ReserveStock;

public sealed class ReserveStockCommandValidator : AbstractValidator<ReserveStockCommand>
{
    public ReserveStockCommandValidator()
    {
        RuleFor(x => x.BatchId)
            .NotEmpty()
            .WithMessage("Inventory batch is required.");

        RuleFor(x => x.Quantity)
            .GreaterThan(0)
            .WithMessage("Quantity must be greater than zero.");

        RuleFor(x => x.DistanceKm)
            .GreaterThanOrEqualTo(0)
            .When(x => x.DistanceKm is not null)
            .WithMessage("Distance cannot be negative.");
    }
}