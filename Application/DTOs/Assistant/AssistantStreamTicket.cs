namespace Application.DTOs.Assistant;

public sealed record AssistantHistoryTurn(string Question, string Answer);

public sealed record AssistantStreamTicket(
    Guid ConversationId,
    Guid StoreId,
    Guid UserId,
    string Question,
    IReadOnlyList<AssistantHistoryTurn> History);