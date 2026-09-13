namespace Application.Abstractions.Services;

public interface IAssistantRateLimiter
{
    bool TryAcquire(Guid userId, out int retryAfterSeconds);
}