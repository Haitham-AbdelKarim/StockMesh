using FluentValidation;

namespace Application.Features.Recommendations.Queries.GetRecommendationForProduct;

public sealed class GetRecommendationForProductQueryValidator : AbstractValidator<GetRecommendationForProductQuery>
{
    public GetRecommendationForProductQueryValidator()
    {
        RuleFor(x => x.ProductId)
            .NotEmpty()
            .WithMessage("Product is required.");
    }
}