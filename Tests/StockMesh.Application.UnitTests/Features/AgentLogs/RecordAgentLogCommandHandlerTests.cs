using Application.Common.Models;
using Application.Features.AgentLogs.Commands.RecordAgentLog;
using FluentAssertions;
using StockMesh.Application.UnitTests.Fakes;

namespace StockMesh.Application.UnitTests.Features.AgentLogs;

public class RecordAgentLogCommandHandlerTests
{
    [Fact]
    public async Task Handle_WithValidCommand_PersistsLog()
    {
        var logRepository = new FakeAgentToolCallLogRepository();
        var handler = new RecordAgentLogCommandHandler(logRepository);
        var conversationId = Guid.NewGuid();

        var result = await handler.Handle(
            new RecordAgentLogCommand(
                Guid.NewGuid(), Guid.NewGuid(), conversationId,
                "What sells best?", """[{"name":"get_top_sellers"}]""", "Paracetamol."),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        logRepository.All.Should().ContainSingle()
            .Which.ConversationId.Should().Be(conversationId);
    }

    [Fact]
    public async Task Validate_WithOversizedQuestion_Fails()
    {
        var validator = new RecordAgentLogCommandValidator();

        var result = validator.Validate(new RecordAgentLogCommand(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            new string('x', 1001), "[]", "Answer."));

        result.IsValid.Should().BeFalse();
    }
}