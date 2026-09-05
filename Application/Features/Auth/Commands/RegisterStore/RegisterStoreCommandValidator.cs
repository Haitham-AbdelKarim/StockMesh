using FluentValidation;

namespace Application.Features.Auth.Commands.RegisterStore;

public sealed class RegisterStoreCommandValidator : AbstractValidator<RegisterStoreCommand>
{
    public RegisterStoreCommandValidator()
    {
        RuleFor(x => x.StoreName)
            .NotEmpty()
            .WithMessage("Store name is required.")
            .MinimumLength(2)
            .MaximumLength(150);

        RuleFor(x => x.VerticalCategory)
            .IsInEnum();

        RuleFor(x => x.Latitude)
            .InclusiveBetween(-90.0, 90.0);

        RuleFor(x => x.Longitude)
            .InclusiveBetween(-180.0, 180.0);

        RuleFor(x => x.MaxSearchRadiusKm)
            .InclusiveBetween(1.0, 500.0);

        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress();

        RuleFor(x => x.Password)
            .NotEmpty()
            .MinimumLength(8);
    }
}