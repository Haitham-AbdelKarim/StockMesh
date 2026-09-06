using FluentValidation;

namespace Application.Features.Inventory.Queries.GetInventoryBatches;

public sealed class GetInventoryBatchesQueryValidator : AbstractValidator<GetInventoryBatchesQuery>
{
    public GetInventoryBatchesQueryValidator()
    {
        RuleFor(x => x.ProductId)
            .NotEmpty()
            .When(x => x.ProductId is not null);

        RuleFor(x => x.Page)
            .GreaterThanOrEqualTo(1);

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100);
    }
}