using FluentValidation;

namespace Application.Features.Dashboard.Queries.GetNetworkSummary;

public sealed class GetNetworkSummaryQueryValidator : AbstractValidator<GetNetworkSummaryQuery>
{
    public GetNetworkSummaryQueryValidator()
    {
        RuleFor(x => x.Days)
            .InclusiveBetween(1, 365)
            .WithMessage("Days must be between 1 and 365.");
    }
}