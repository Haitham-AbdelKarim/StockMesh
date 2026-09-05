using Application.Common.Models;
using FluentValidation;
using MediatR;

namespace Application.Common.Behaviors;

public sealed class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (_validators.Any())
        {
            var context = new ValidationContext<TRequest>(request);

            var validationResults = await Task.WhenAll(
                _validators.Select(v => v.ValidateAsync(context, cancellationToken)));

            var failures = validationResults
                .SelectMany(r => r.Errors)
                .Where(f => f is not null)
                .ToList();

            if (failures.Count != 0)
            {
                var errors = failures
                    .GroupBy(f => f.PropertyName ?? string.Empty)
                    .ToDictionary(
                        group => group.Key,
                        group => group.Select(f => f.ErrorMessage).ToArray());

                var failure = ResultFactory.CreateValidationFailure(typeof(TResponse), errors);

                if (failure is not null)
                {
                    return (TResponse)failure;
                }

                throw new ValidationException(failures);
            }
        }

        return await next();
    }
}