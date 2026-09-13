using FluentAssertions;
using Infrastructure.Assistant;
using Infrastructure.ExternalServices;
using Microsoft.Extensions.Caching.Memory;

namespace StockMesh.Infrastructure.UnitTests.Assistant;

public class AssistantRateLimiterTests : IDisposable
{
    private readonly IMemoryCache _cache = new MemoryCache(new MemoryCacheOptions());

    private readonly AssistantSettings _settings = new()
    {
        BaseUrl = "http://localhost:8001",
        MaxQuestionsPerMinute = 3,
    };

    public void Dispose()
    {
        _cache.Dispose();
    }

    [Fact]
    public void TryAcquire_WithinQuota_Allows()
    {
        var limiter = CreateLimiter();
        var userId = Guid.NewGuid();

        for (var i = 0; i < 3; i++)
        {
            limiter.TryAcquire(userId, out _).Should().BeTrue();
        }
    }

    [Fact]
    public void TryAcquire_BeyondQuota_RejectsWithRetryAfter()
    {
        var limiter = CreateLimiter();
        var userId = Guid.NewGuid();

        for (var i = 0; i < 3; i++)
        {
            limiter.TryAcquire(userId, out _);
        }

        limiter.TryAcquire(userId, out var retryAfter).Should().BeFalse();
        retryAfter.Should().BeInRange(1, 60);
    }

    [Fact]
    public void TryAcquire_DifferentUsers_HaveIndependentQuotas()
    {
        var limiter = CreateLimiter();

        for (var i = 0; i < 3; i++)
        {
            limiter.TryAcquire(Guid.NewGuid(), out _).Should().BeTrue();
        }

        limiter.TryAcquire(Guid.NewGuid(), out _).Should().BeTrue();
    }

    private AssistantRateLimiter CreateLimiter()
    {
        return new AssistantRateLimiter(_cache, _settings);
    }
}