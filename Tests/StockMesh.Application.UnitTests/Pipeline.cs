using Application;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using StockMesh.Application.UnitTests.Samples;

namespace StockMesh.Application.UnitTests;

internal static class Pipeline
{
    public static TestPipeline Create()
    {
        var logger = new TestLogger();
        var services = new ServiceCollection();

        services.AddLogging(builder => builder
            .ClearProviders()
            .AddProvider(new TestLoggerProvider(logger)));

        services.AddApplication();
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(SampleCommand).Assembly));
        services.AddValidatorsFromAssembly(typeof(SampleCommandValidator).Assembly);

        var provider = services.BuildServiceProvider();
        return new TestPipeline(provider.GetRequiredService<IMediator>(), logger);
    }
}

internal sealed class TestPipeline
{
    public TestPipeline(IMediator mediator, TestLogger logger)
    {
        Mediator = mediator;
        Logger = logger;
    }

    public IMediator Mediator { get; }

    public TestLogger Logger { get; }
}