using FluentValidation;

namespace Application.Features.Products.Queries.GetProducts;

public sealed class GetProductsQueryValidator : AbstractValidator<GetProductsQuery>
{
    public GetProductsQueryValidator()
    {
        RuleFor(x => x.VerticalCategory)
            .IsInEnum()
            .When(x => x.VerticalCategory is not null);

        RuleFor(x => x.Search)
            .MaximumLength(150)
            .When(x => x.Search is not null);

        RuleFor(x => x.Page)
            .GreaterThanOrEqualTo(1);

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100);
    }
}