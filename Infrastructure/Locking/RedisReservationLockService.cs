using Application.Abstractions.Locking;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace Infrastructure.Locking;

public sealed class RedisReservationLockService : IReservationLockService
{
    private readonly Lazy<ConnectionMultiplexer> _multiplexer;
    private readonly ILogger<RedisReservationLockService> _logger;
    private readonly TimeSpan _lockTtl = TimeSpan.FromSeconds(10);

    public RedisReservationLockService(
        IConfiguration configuration,
        ILogger<RedisReservationLockService> logger)
    {
        var configurationString =
            configuration["Redis:Configuration"] ?? "localhost:6379,abortConnect=false";

        _multiplexer = new Lazy<ConnectionMultiplexer>(() =>
            ConnectionMultiplexer.Connect(configurationString));
        _logger = logger;
    }

    public async Task<bool> AcquireAsync(
        Guid batchId,
        string token,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var database = _multiplexer.Value.GetDatabase();

            return await database.StringSetAsync(Key(batchId), token, _lockTtl, When.NotExists);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to acquire reservation lock for batch {BatchId}.", batchId);

            return false;
        }
    }

    public async Task ReleaseAsync(
        Guid batchId,
        string token,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var database = _multiplexer.Value.GetDatabase();

            bool released = await database.LockReleaseAsync(Key(batchId), token);
            if (!released)
            {
                _logger.LogWarning("Reservation lock for batch {BatchId} was not held by the expected owner at release time; it will expire via TTL.", batchId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to release reservation lock for batch {BatchId}.", batchId);
        }
    }

    private static string Key(Guid batchId)
    {
        return $"stockmesh:lock:{batchId}";
    }
}