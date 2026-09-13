using FluentValidation;

namespace Application.Features.Expenses.Commands.RecordExpense;

public sealed class RecordExpenseCommandValidator : AbstractValidator<RecordExpenseCommand>
{
    public RecordExpenseCommandValidator()
    {
        RuleFor(x => x.Category)
            .NotEmpty()
            .WithMessage("Category is required.")
            .MaximumLength(64)
            .WithMessage("Category must not exceed 64 characters.");

        RuleFor(x => x.Amount)
            .GreaterThan(0)
            .WithMessage("Amount must be greater than zero.");

        RuleFor(x => x.IncurredAt)
            .NotEmpty()
            .WithMessage("IncurredAt is required.");
    }
}