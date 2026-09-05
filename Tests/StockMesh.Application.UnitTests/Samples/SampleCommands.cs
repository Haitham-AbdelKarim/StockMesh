using Application.Common.Models;
using FluentValidation;
using MediatR;

namespace StockMesh.Application.UnitTests.Samples;

public sealed record SampleCommand(string? Name, string? Secret = null) : IRequest<Unit>;

public sealed class SampleCommandHandler : IRequestHandler<SampleCommand, Unit>
{
    public static bool Executed;

    public Task<Unit> Handle(SampleCommand request, CancellationToken cancellationToken)
    {
        Executed = true;
        return Task.FromResult(Unit.Value);
    }
}

public sealed class SampleCommandValidator : AbstractValidator<SampleCommand>
{
    public SampleCommandValidator()
    {
        RuleFor(command => command.Name).NotEmpty();
    }
}

public sealed record SampleThrowingRequest : IRequest<Unit>;

public sealed class SampleThrowingHandler : IRequestHandler<SampleThrowingRequest, Unit>
{
    public Task<Unit> Handle(SampleThrowingRequest request, CancellationToken cancellationToken)
    {
        throw new InvalidOperationException("Sample failure.");
    }
}

public enum SampleResultKind
{
    Success,
    Plain,
    NotFound,
    Conflict
}

public sealed record SampleResultRequest(SampleResultKind Kind) : IRequest<Result<SampleResultPayload>>;

public sealed class SampleResultPayload
{
    public int Id { get; init; } = 1;
}

public sealed class SampleResultHandler : IRequestHandler<SampleResultRequest, Result<SampleResultPayload>>
{
    public Task<Result<SampleResultPayload>> Handle(
        SampleResultRequest request,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(
            request.Kind switch
            {
                SampleResultKind.NotFound => Result<SampleResultPayload>.NotFound("Sample not found."),
                SampleResultKind.Conflict => Result<SampleResultPayload>.Conflict("Sample already exists."),
                SampleResultKind.Plain => Result<SampleResultPayload>.Failure("Sample invalid."),
                _ => Result<SampleResultPayload>.Success(new SampleResultPayload())
            });
    }
}

public sealed record ValidatedResultCommand(string? Name) : IRequest<Result<SampleResultPayload>>;

public sealed class ValidatedResultCommandHandler : IRequestHandler<ValidatedResultCommand, Result<SampleResultPayload>>
{
    public static bool Executed;

    public Task<Result<SampleResultPayload>> Handle(
        ValidatedResultCommand request,
        CancellationToken cancellationToken)
    {
        Executed = true;
        return Task.FromResult(Result<SampleResultPayload>.Success(new SampleResultPayload()));
    }
}

public sealed class ValidatedResultCommandValidator : AbstractValidator<ValidatedResultCommand>
{
    public ValidatedResultCommandValidator()
    {
        RuleFor(command => command.Name).NotEmpty();
    }
}