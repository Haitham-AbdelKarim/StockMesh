using Application.Abstractions.Options;
using Application.Features.Recommendations.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Infrastructure.BackgroundJobs;

public sealed class RecommendationSweeper : BackgroundService
{
    // Single-replica assumption (see Project Documentation §11): if the API is
    // ever scaled horizontally, guard each run with a Redis-backed distributed
    // lock so the nightly generation executes exactly once.
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<RecommendationSweeper> _logger;
    private readonly TimeSpan _interval;

    public RecommendationSweeper(
        IServiceScopeFactory scopeFactory,
        RecommendationOptions options,
        ILogger<RecommendationSweeper> logger)
    {
        _scopeFactory = scopeFactory;
        _interval = TimeSpan.FromHours(options.SweepIntervalHours);
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(_interval, stoppingToken);

            try
            {
                using var scope = _scopeFactory.CreateScope();

                var generator = scope.ServiceProvider.GetRequiredService<RecommendationGenerator>();

                await generator.RunAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Recommendation generation failed.");
            }
        }
    }
}