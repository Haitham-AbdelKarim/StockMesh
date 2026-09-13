using FluentValidation;

namespace Application.Features.Dashboard.Queries.GetTopSellers;

public sealed class GetTopSellersQueryValidator : AbstractValidator<GetTopSellersQuery>
{
    public GetTopSellersQueryValidator()
    {
        RuleFor(x => x.TopN)
            .InclusiveBetween(1, 50)
            .WithMessage("TopN must be between 1 and 50.");

        RuleFor(x => x.From)
            .LessThanOrEqualTo(x => x.To)
            .When(x => x.From.HasValue && x.To.HasValue)
            .WithMessage("From must not be after To.");
    }
}