using Application.Abstractions.Options;
using Application.Features.Reservations.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Infrastructure.BackgroundJobs;

public sealed class ReservationExpirySweeper : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ReservationExpirySweeper> _logger;
    private readonly TimeSpan _interval;

    public ReservationExpirySweeper(
        IServiceScopeFactory scopeFactory,
        ReservationOptions options,
        ILogger<ReservationExpirySweeper> logger)
    {
        _scopeFactory = scopeFactory;
        _interval = TimeSpan.FromSeconds(options.ExpirySweepIntervalSeconds);
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

                var processor = scope.ServiceProvider.GetRequiredService<ReservationExpiryProcessor>();

                await processor.RunAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Reservation expiry sweep failed.");
            }
        }
    }
}