using Domain.Common;

namespace Domain.Entities;

public class AgentToolCallLog : BaseEntity
{
    public Guid StoreId { get; private set; }

    public Guid UserId { get; private set; }

    public Guid ConversationId { get; private set; }

    public string Question { get; private set; } = string.Empty;

    public string ToolCalls { get; private set; } = string.Empty;

    public string FinalAnswer { get; private set; } = string.Empty;

    private AgentToolCallLog()
    {
    }

    public AgentToolCallLog(
        Guid storeId,
        Guid userId,
        Guid conversationId,
        string question,
        string toolCalls,
        string finalAnswer,
        DateTime? createdAt = null)
    {
        if (string.IsNullOrWhiteSpace(question))
        {
            throw new ArgumentException("Question is required.", nameof(question));
        }

        StoreId = storeId;
        UserId = userId;
        ConversationId = conversationId;
        Question = question;
        ToolCalls = toolCalls;
        FinalAnswer = finalAnswer;

        if (createdAt.HasValue)
        {
            CreatedAt = createdAt.Value;
        }
    }
}