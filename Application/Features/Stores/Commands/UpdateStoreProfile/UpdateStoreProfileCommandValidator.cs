using FluentValidation;

namespace Application.Features.Stores.Commands.UpdateStoreProfile;

public sealed class UpdateStoreProfileCommandValidator : AbstractValidator<UpdateStoreProfileCommand>
{
    public UpdateStoreProfileCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Store name is required.")
            .MinimumLength(2)
            .MaximumLength(150);

        RuleFor(x => x.Latitude)
            .InclusiveBetween(-90.0, 90.0);

        RuleFor(x => x.Longitude)
            .InclusiveBetween(-180.0, 180.0);

        RuleFor(x => x.MaxSearchRadiusKm)
            .InclusiveBetween(1.0, 500.0);
    }
}