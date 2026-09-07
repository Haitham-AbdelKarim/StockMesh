using Domain.Common;

namespace Domain.Entities;

public class Expense : BaseEntity
{
    public Guid StoreId { get; private set; }

    public string Category { get; private set; } = string.Empty;

    public decimal Amount { get; private set; }

    public DateTime IncurredAt { get; private set; }

    public string? Note { get; private set; }

    private Expense()
    {
    }

    public Expense(
        Guid storeId,
        string category,
        decimal amount,
        DateTime incurredAt,
        string? note = null)
    {
        StoreId = storeId;
        Category = category;
        Amount = amount;
        IncurredAt = incurredAt;
        Note = note;
    }
}