using FluentValidation;

namespace Application.Features.Dashboard.Queries.GetSalesTrend;

public sealed class GetSalesTrendQueryValidator : AbstractValidator<GetSalesTrendQuery>
{
    public GetSalesTrendQueryValidator()
    {
        RuleFor(x => x.Days)
            .InclusiveBetween(1, 365)
            .WithMessage("Days must be between 1 and 365.");
    }
}