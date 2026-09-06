using FluentValidation;

namespace Application.Features.StockMovements.Commands.RecordRestock;

public sealed class RecordRestockCommandValidator : AbstractValidator<RecordRestockCommand>
{
    public RecordRestockCommandValidator()
    {
        RuleFor(x => x.ProductId)
            .NotEmpty()
            .WithMessage("Product is required.");

        RuleFor(x => x.Quantity)
            .GreaterThan(0)
            .WithMessage("Quantity must be greater than zero.");

        RuleFor(x => x.UnitCost)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Unit cost cannot be negative.");
    }
}