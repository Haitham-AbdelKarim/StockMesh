using FluentValidation;

namespace Application.Features.Inventory.Queries.GetBatchById;

public sealed class GetBatchByIdQueryValidator : AbstractValidator<GetBatchByIdQuery>
{
    public GetBatchByIdQueryValidator()
    {
        RuleFor(x => x.BatchId)
            .NotEmpty()
            .WithMessage("Inventory batch is required.");
    }
}