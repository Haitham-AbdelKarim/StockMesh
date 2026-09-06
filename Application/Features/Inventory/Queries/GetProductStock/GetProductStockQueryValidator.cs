using FluentValidation;

namespace Application.Features.Inventory.Queries.GetProductStock;

public sealed class GetProductStockQueryValidator : AbstractValidator<GetProductStockQuery>
{
    public GetProductStockQueryValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThanOrEqualTo(1);

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100);
    }
}