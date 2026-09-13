using Application.Common.Models;
using Application.Features.Assistant.Commands.AskAssistant;
using Domain.Entities;
using Domain.Enums;
using FluentAssertions;
using StockMesh.Application.UnitTests.Fakes;

namespace StockMesh.Application.UnitTests.Features.Assistant;

public class AskAssistantCommandHandlerTests
{
    private static readonly Guid StoreId = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly FakeDateTimeProvider Clock = new();

    [Fact]
    public async Task Handle_WithoutConversationId_CreatesConversationAndReturnsTicket()
    {
        var conversationRepository = new FakeConversationRepository();
        var handler = CreateHandler(conversationRepository, new FakeAgentToolCallLogRepository());

        var result = await handler.Handle(
            new AskAssistantCommand("How can I increase profit?"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Question.Should().Be("How can I increase profit?");
        result.Value.History.Should().BeEmpty();
        result.Value.StoreId.Should().Be(StoreId);
        result.Value.UserId.Should().Be(UserId);
    }

    [Fact]
    public async Task Handle_WithOwnConversationId_ReusesItAndLoadsHistory()
    {
        var conversation = new Conversation(StoreId, UserId, "Old question", Clock.UtcNow);
        var log = new AgentToolCallLog(
            StoreId, UserId, conversation.Id, "Old question?", "[]", "Old answer.",
            Clock.UtcNow);
        var handler = CreateHandler(
            new FakeConversationRepository(conversation),
            new FakeAgentToolCallLogRepository(log));

        var result = await handler.Handle(
            new AskAssistantCommand("Follow-up?", conversation.Id),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.ConversationId.Should().Be(conversation.Id);
        result.Value.History.Should().ContainSingle()
            .Which.Should().BeEquivalentTo(new { Question = "Old question?", Answer = "Old answer." });
    }

    [Fact]
    public async Task Handle_WithForeignConversationId_ReturnsNotFound()
    {
        var foreign = new Conversation(Guid.NewGuid(), Guid.NewGuid(), "Someone else", Clock.UtcNow);
        var handler = CreateHandler(
            new FakeConversationRepository(foreign),
            new FakeAgentToolCallLogRepository());

        var result = await handler.Handle(
            new AskAssistantCommand("Hi?", foreign.Id),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Kind.Should().Be(FailureKind.NotFound);
    }

    [Fact]
    public async Task Handle_WithUnknownConversationId_ReturnsNotFound()
    {
        var handler = CreateHandler(
            new FakeConversationRepository(),
            new FakeAgentToolCallLogRepository());

        var result = await handler.Handle(
            new AskAssistantCommand("Hi?", Guid.NewGuid()),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Kind.Should().Be(FailureKind.NotFound);
    }

    [Fact]
    public async Task Handle_WhenRateLimited_ReturnsRateLimited()
    {
        var rateLimiter = new FakeAssistantRateLimiter { Allow = false, RetryAfterSeconds = 42 };
        var handler = CreateHandler(
            new FakeConversationRepository(),
            new FakeAgentToolCallLogRepository(),
            rateLimiter);

        var result = await handler.Handle(
            new AskAssistantCommand("Hi?"),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Kind.Should().Be(FailureKind.RateLimited);
        result.Error.Should().Contain("42");
    }

    [Fact]
    public async Task Validate_WithEmptyQuestion_Fails()
    {
        var validator = new AskAssistantCommandValidator();

        validator.Validate(new AskAssistantCommand(string.Empty)).IsValid.Should().BeFalse();
        validator.Validate(new AskAssistantCommand(new string('x', 1001))).IsValid.Should().BeFalse();
        validator.Validate(new AskAssistantCommand("Valid?")).IsValid.Should().BeTrue();
    }

    private static AskAssistantCommandHandler CreateHandler(
        FakeConversationRepository conversationRepository,
        FakeAgentToolCallLogRepository logRepository,
        FakeAssistantRateLimiter? rateLimiter = null)
    {
        return new AskAssistantCommandHandler(
            new FakeCurrentUser
            {
                StoreId = StoreId,
                UserId = UserId,
                VerticalCategory = VerticalCategory.Pharmacy,
            },
            Clock,
            conversationRepository,
            logRepository,
            rateLimiter ?? new FakeAssistantRateLimiter());
    }
}