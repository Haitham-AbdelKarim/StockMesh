using Application.Common.Models;
using Application.Features.Assistant.Queries.GetConversationById;
using Application.Features.Assistant.Queries.GetConversations;
using Domain.Entities;
using Domain.Enums;
using FluentAssertions;
using StockMesh.Application.UnitTests.Fakes;

namespace StockMesh.Application.UnitTests.Features.Assistant;

public class ConversationQueryTests
{
    private static readonly Guid StoreId = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly FakeDateTimeProvider Clock = new();

    [Fact]
    public async Task Handle_GetConversations_ReturnsOnlyCallerThreadsNewestFirst()
    {
        var mine1 = new Conversation(StoreId, UserId, "First", Clock.UtcNow.AddHours(-2));
        var mine2 = new Conversation(StoreId, UserId, "Second", Clock.UtcNow);
        var foreign = new Conversation(Guid.NewGuid(), Guid.NewGuid(), "Foreign", Clock.UtcNow);
        var handler = new GetConversationsQueryHandler(
            CurrentUser(),
            new FakeConversationRepository(mine1, mine2, foreign));

        var result = await handler.Handle(new GetConversationsQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Select(c => c.Title).Should().Equal("Second", "First");
    }

    [Fact]
    public async Task Handle_GetConversationById_ReturnsTurnsInOrder()
    {
        var conversation = new Conversation(StoreId, UserId, "Chat", Clock.UtcNow);
        var older = new AgentToolCallLog(
            StoreId, UserId, conversation.Id, "Q1?", "[]", "A1.",
            Clock.UtcNow.AddMinutes(-5));
        var newer = new AgentToolCallLog(
            StoreId, UserId, conversation.Id, "Q2?", "[]", "A2.",
            Clock.UtcNow);
        var handler = new GetConversationByIdQueryHandler(
            CurrentUser(),
            new FakeConversationRepository(conversation),
            new FakeAgentToolCallLogRepository(older, newer));

        var result = await handler.Handle(
            new GetConversationByIdQuery(conversation.Id),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Title.Should().Be("Chat");
        result.Value.Turns.Select(t => t.Question).Should().Equal("Q1?", "Q2?");
    }

    [Fact]
    public async Task Handle_GetConversationById_WithForeignId_ReturnsNotFound()
    {
        var foreign = new Conversation(Guid.NewGuid(), Guid.NewGuid(), "Foreign", Clock.UtcNow);
        var handler = new GetConversationByIdQueryHandler(
            CurrentUser(),
            new FakeConversationRepository(foreign),
            new FakeAgentToolCallLogRepository());

        var result = await handler.Handle(
            new GetConversationByIdQuery(foreign.Id),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Kind.Should().Be(FailureKind.NotFound);
    }

    private static FakeCurrentUser CurrentUser()
    {
        return new FakeCurrentUser
        {
            StoreId = StoreId,
            UserId = UserId,
            VerticalCategory = VerticalCategory.Pharmacy,
        };
    }
}