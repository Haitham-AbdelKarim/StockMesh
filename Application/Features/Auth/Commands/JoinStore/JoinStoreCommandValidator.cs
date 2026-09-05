using FluentValidation;

namespace Application.Features.Auth.Commands.JoinStore;

public sealed class JoinStoreCommandValidator : AbstractValidator<JoinStoreCommand>
{
    public JoinStoreCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress();

        RuleFor(x => x.Password)
            .NotEmpty()
            .MinimumLength(8);
    }
}