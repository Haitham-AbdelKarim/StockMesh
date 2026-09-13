using Domain.Entities;
using Domain.Enums;
using FluentAssertions;

namespace StockMesh.Domain.UnitTests.Entities;

public class ConversationEntityTests
{
    [Fact]
    public void Conversation_truncates_long_titles_to_80_chars()
    {
        var conversation = new Conversation(
            Guid.NewGuid(), Guid.NewGuid(), new string('x', 200));

        conversation.Title.Should().HaveLength(80);
    }

    [Fact]
    public void Conversation_rejects_empty_title()
    {
        var act = () => new Conversation(Guid.NewGuid(), Guid.NewGuid(), "  ");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void AgentToolCallLog_rejects_empty_question()
    {
        var act = () => new AgentToolCallLog(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "  ", "[]", "Answer.");

        act.Should().Throw<ArgumentException>();
    }
}