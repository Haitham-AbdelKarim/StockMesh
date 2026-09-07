using FluentValidation;

namespace Application.Features.Dashboard.Queries.GetDashboardSummary;

public sealed class GetDashboardSummaryQueryValidator : AbstractValidator<GetDashboardSummaryQuery>
{
    public GetDashboardSummaryQueryValidator()
    {
        RuleFor(x => x.Date)
            .NotEmpty()
            .When(x => x.Date.HasValue)
            .WithMessage("Date must be valid.");
    }
}