using Application.Common.Models;
using FluentAssertions;
using FluentValidation;
using StockMesh.Application.UnitTests.Samples;

namespace StockMesh.Application.UnitTests;

public class PipelineBehaviorTests
{
    [Fact]
    public async Task ValidRequest_PassesThroughPipeline()
    {
        var pipeline = Pipeline.Create();
        SampleCommandHandler.Executed = false;

        await pipeline.Mediator.Send(new SampleCommand("valid"));

        SampleCommandHandler.Executed.Should().BeTrue();
        pipeline.Logger.Entries.Should().Contain(entry => entry.Contains("Handling SampleCommand"));
        pipeline.Logger.Entries.Should().Contain(entry => entry.Contains("Handled SampleCommand"));
    }

    [Fact]
    public async Task InvalidRequest_ValidationShortCircuitsHandler()
    {
        var pipeline = Pipeline.Create();
        SampleCommandHandler.Executed = false;

        Func<Task> act = () => pipeline.Mediator.Send(new SampleCommand(null));

        await act.Should().ThrowAsync<ValidationException>();
        SampleCommandHandler.Executed.Should().BeFalse();
    }

    [Fact]
    public async Task HandlerException_IsLoggedAndRethrown()
    {
        var pipeline = Pipeline.Create();

        Func<Task> act = () => pipeline.Mediator.Send(new SampleThrowingRequest());

        await act.Should().ThrowAsync<InvalidOperationException>();
        pipeline.Logger.Entries.Should().Contain(entry =>
            entry.Contains("Unhandled exception for request SampleThrowingRequest"));
    }

    [Fact]
    public async Task LoggingBehavior_NeverLogsRequestPayload()
    {
        var pipeline = Pipeline.Create();

        await pipeline.Mediator.Send(new SampleCommand("valid", "S3cr3t!"));

        pipeline.Logger.Entries.Should().NotContain(entry => entry.Contains("S3cr3t!"));
    }

    [Fact]
    public async Task ResultSuccess_ReturnsValue()
    {
        var pipeline = Pipeline.Create();

        var result = await pipeline.Mediator.Send(new SampleResultRequest(SampleResultKind.Success));

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Id.Should().Be(1);
    }

    [Fact]
    public async Task ResultNotFound_ReturnsFailedResultWithoutThrowing()
    {
        var pipeline = Pipeline.Create();

        var result = await pipeline.Mediator.Send(new SampleResultRequest(SampleResultKind.NotFound));

        result.IsSuccess.Should().BeFalse();
        result.Kind.Should().Be(FailureKind.NotFound);
        result.Error.Should().Be("Sample not found.");
    }

    [Fact]
    public async Task ResultConflict_ReturnsFailedResultWithoutThrowing()
    {
        var pipeline = Pipeline.Create();

        var result = await pipeline.Mediator.Send(new SampleResultRequest(SampleResultKind.Conflict));

        result.IsSuccess.Should().BeFalse();
        result.Kind.Should().Be(FailureKind.Conflict);
        result.Error.Should().Be("Sample already exists.");
    }

    [Fact]
    public async Task ResultPlainFailure_ReturnsFailedResult_WithBadRequestAsDefaultKind()
    {
        var pipeline = Pipeline.Create();

        var result = await pipeline.Mediator.Send(new SampleResultRequest(SampleResultKind.Plain));

        result.IsSuccess.Should().BeFalse();
        result.Kind.Should().BeNull();
        result.Error.Should().Be("Sample invalid.");
    }

    [Fact]
    public async Task ResultRequest_ValidationFailure_ReturnsFailedResultAndShortCircuitsHandler()
    {
        var pipeline = Pipeline.Create();
        ValidatedResultCommandHandler.Executed = false;

        var result = await pipeline.Mediator.Send(new ValidatedResultCommand(null));

        result.IsSuccess.Should().BeFalse();
        result.Kind.Should().Be(FailureKind.Validation);
        result.ValidationErrors.Should().ContainKey("Name");
        ValidatedResultCommandHandler.Executed.Should().BeFalse();
    }
}