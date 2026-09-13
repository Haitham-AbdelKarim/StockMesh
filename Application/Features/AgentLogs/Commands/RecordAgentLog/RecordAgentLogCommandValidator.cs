using FluentValidation;

namespace Application.Features.AgentLogs.Commands.RecordAgentLog;

public sealed class RecordAgentLogCommandValidator : AbstractValidator<RecordAgentLogCommand>
{
    public RecordAgentLogCommandValidator()
    {
        RuleFor(x => x.StoreId).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.ConversationId).NotEmpty();

        RuleFor(x => x.Question)
            .NotEmpty().WithMessage("Question is required.")
            .MaximumLength(1000).WithMessage("Question must be at most 1000 characters.");

        RuleFor(x => x.ToolCalls)
            .NotNull().WithMessage("Tool calls are required.")
            .MaximumLength(8000).WithMessage("Tool calls must be at most 8000 characters.");

        RuleFor(x => x.FinalAnswer)
            .NotEmpty().WithMessage("Final answer is required.")
            .MaximumLength(8000).WithMessage("Final answer must be at most 8000 characters.");
    }
}