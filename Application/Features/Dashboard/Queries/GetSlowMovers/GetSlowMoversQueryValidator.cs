using FluentValidation;

namespace Application.Features.Dashboard.Queries.GetSlowMovers;

public sealed class GetSlowMoversQueryValidator : AbstractValidator<GetSlowMoversQuery>
{
    public GetSlowMoversQueryValidator()
    {
        RuleFor(x => x.Days)
            .InclusiveBetween(7, 365)
            .WithMessage("Days must be between 7 and 365.");
    }
}