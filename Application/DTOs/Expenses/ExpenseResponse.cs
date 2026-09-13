namespace Application.DTOs.Expenses;

public sealed record ExpenseResponse(
    Guid Id,
    Guid StoreId,
    string Category,
    decimal Amount,
    DateTime IncurredAt,
    string? Note);