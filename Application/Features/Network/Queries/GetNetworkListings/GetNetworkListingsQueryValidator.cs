using FluentValidation;

namespace Application.Features.Network.Queries.GetNetworkListings;

public sealed class GetNetworkListingsQueryValidator : AbstractValidator<GetNetworkListingsQuery>
{
    public GetNetworkListingsQueryValidator()
    {
        RuleFor(x => x.Category)
            .IsInEnum()
            .When(x => x.Category is not null);

        RuleFor(x => x.MaxDistanceKm)
            .InclusiveBetween(1.0, 500.0)
            .When(x => x.MaxDistanceKm is not null);

        RuleFor(x => x.Search)
            .MaximumLength(150)
            .When(x => x.Search is not null);

        RuleFor(x => x.Page)
            .GreaterThanOrEqualTo(1);

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100);
    }
}