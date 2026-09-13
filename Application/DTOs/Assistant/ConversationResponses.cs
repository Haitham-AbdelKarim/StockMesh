namespace Application.DTOs.Assistant;

public sealed record ConversationResponse(
    Guid Id,
    string Title,
    DateTime LastActiveAt);

public sealed record ConversationTurnResponse(
    string Question,
    string ToolCalls,
    string Answer,
    DateTime CreatedAt);

public sealed record ConversationDetailResponse(
    Guid Id,
    string Title,
    IReadOnlyList<ConversationTurnResponse> Turns);