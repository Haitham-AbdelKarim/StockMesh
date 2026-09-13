using Domain.Common;

namespace Domain.Entities;

public class ProcessedStripeEvent : BaseEntity
{
    public string EventId { get; private set; } = string.Empty;

    private ProcessedStripeEvent()
    {
    }

    public ProcessedStripeEvent(string eventId, DateTime? receivedAt = null)
    {
        if (string.IsNullOrWhiteSpace(eventId))
        {
            throw new ArgumentException("Stripe event id is required.", nameof(eventId));
        }

        EventId = eventId;
        CreatedAt = receivedAt ?? DateTime.UtcNow;
    }
}