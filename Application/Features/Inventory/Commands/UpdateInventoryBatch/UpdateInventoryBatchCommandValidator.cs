using FluentValidation;

namespace Application.Features.Inventory.Commands.UpdateInventoryBatch;

public sealed class UpdateInventoryBatchCommandValidator : AbstractValidator<UpdateInventoryBatchCommand>
{
    public UpdateInventoryBatchCommandValidator()
    {
        RuleFor(x => x.BatchId)
            .NotEmpty()
            .WithMessage("Inventory batch is required.");

        RuleFor(x => x.UnitSalePrice)
            .GreaterThanOrEqualTo(0)
            .When(x => x.UnitSalePrice is not null)
            .WithMessage("Unit sale price cannot be negative.");

        RuleFor(x => x.ReorderPoint)
            .GreaterThanOrEqualTo(0)
            .When(x => x.ReorderPoint is not null)
            .WithMessage("Reorder point cannot be negative.");

        RuleFor(x => x.LeadTimeDays)
            .GreaterThanOrEqualTo(0)
            .When(x => x.LeadTimeDays is not null)
            .WithMessage("Lead time cannot be negative.");
    }
}