using Domain.Common;

namespace Domain.Entities;

public class Conversation : BaseEntity
{
    public Guid StoreId { get; private set; }

    public Guid UserId { get; private set; }

    public string Title { get; private set; } = string.Empty;

    public DateTime LastActiveAt { get; private set; }

    private Conversation()
    {
    }

    public Conversation(Guid storeId, Guid userId, string title, DateTime? createdAt = null)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException("Conversation title is required.", nameof(title));
        }

        StoreId = storeId;
        UserId = userId;
        Title = title.Length > 80 ? title[..80] : title;
        LastActiveAt = createdAt ?? DateTime.UtcNow;

        if (createdAt.HasValue)
        {
            CreatedAt = createdAt.Value;
        }
    }

    public void Touch(DateTime? now = null)
    {
        LastActiveAt = now ?? DateTime.UtcNow;
    }
}