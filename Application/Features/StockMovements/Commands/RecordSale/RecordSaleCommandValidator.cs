using FluentValidation;

namespace Application.Features.StockMovements.Commands.RecordSale;

public sealed class RecordSaleCommandValidator : AbstractValidator<RecordSaleCommand>
{
    public RecordSaleCommandValidator()
    {
        RuleFor(x => x.BatchId)
            .NotEmpty()
            .WithMessage("Inventory batch is required.");

        RuleFor(x => x.Quantity)
            .GreaterThan(0)
            .WithMessage("Quantity must be greater than zero.");
    }
}