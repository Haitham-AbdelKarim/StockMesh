using Application.Abstractions.Services;
using Infrastructure.ExternalServices;
using Microsoft.Extensions.Caching.Memory;

namespace Infrastructure.Assistant;

public sealed class AssistantRateLimiter : IAssistantRateLimiter
{
    private readonly IMemoryCache _cache;
    private readonly AssistantSettings _settings;

    public AssistantRateLimiter(IMemoryCache cache, AssistantSettings settings)
    {
        _cache = cache;
        _settings = settings;
    }

    public bool TryAcquire(Guid userId, out int retryAfterSeconds)
    {
        var key = $"assistant-ratelimit:{userId:N}";
        var now = DateTimeOffset.UtcNow;
        var window = TimeSpan.FromMinutes(1);

        if (_cache.TryGetValue<(int Count, DateTimeOffset StartedAt)>(key, out var entry)
            && now - entry.StartedAt < window)
        {
            if (entry.Count >= _settings.MaxQuestionsPerMinute)
            {
                retryAfterSeconds = (int)Math.Ceiling((window - (now - entry.StartedAt)).TotalSeconds);

                return false;
            }

            _cache.Set(key, (entry.Count + 1, entry.StartedAt), window);

            retryAfterSeconds = 0;

            return true;
        }

        _cache.Set(key, (1, now), window);

        retryAfterSeconds = 0;

        return true;
    }
}