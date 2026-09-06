using FluentValidation;

namespace Application.Features.Inventory.Commands.AddInventoryBatch;

public sealed class AddInventoryBatchCommandValidator : AbstractValidator<AddInventoryBatchCommand>
{
    public AddInventoryBatchCommandValidator()
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

        RuleFor(x => x.UnitSalePrice)
            .GreaterThanOrEqualTo(0)
            .When(x => x.UnitSalePrice is not null)
            .WithMessage("Unit sale price cannot be negative.");
    }
}