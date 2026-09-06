using FluentValidation;

namespace Application.Features.Inventory.Commands.ToggleBatchSharing;

public sealed class ToggleBatchSharingCommandValidator : AbstractValidator<ToggleBatchSharingCommand>
{
    public ToggleBatchSharingCommandValidator()
    {
        RuleFor(x => x.BatchId)
            .NotEmpty()
            .WithMessage("Inventory batch is required.");

        RuleFor(x => x.SharedQuantity)
            .GreaterThan(0)
            .When(x => x.IsShared)
            .WithMessage("Shared quantity must be greater than zero.");

        RuleFor(x => x.SharedQuantity)
            .GreaterThanOrEqualTo(0)
            .When(x => !x.IsShared)
            .WithMessage("Shared quantity cannot be negative.");
    }
}