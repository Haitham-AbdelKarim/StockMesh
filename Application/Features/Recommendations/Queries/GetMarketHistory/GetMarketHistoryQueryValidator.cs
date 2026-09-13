using FluentValidation;

namespace Application.Features.Recommendations.Queries.GetMarketHistory;

public sealed class GetMarketHistoryQueryValidator : AbstractValidator<GetMarketHistoryQuery>
{
    public GetMarketHistoryQueryValidator()
    {
        RuleFor(x => x.ProductId)
            .NotEmpty()
            .WithMessage("Product is required.");

        RuleFor(x => x.Days)
            .InclusiveBetween(1, 365)
            .WithMessage("Days must be between 1 and 365.");
    }
}