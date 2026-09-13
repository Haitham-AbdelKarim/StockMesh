using Application.Features.Assistant.Services;
using FluentAssertions;

namespace StockMesh.Application.UnitTests.Features.Assistant;

public class HistoryTrimmerTests
{
    [Fact]
    public void Trim_WithFewTurns_KeepsAllInOrder()
    {
        var turns = new[]
        {
            ("q1", "a1"),
            ("q2", "a2"),
        };

        var result = HistoryTrimmer.Trim(turns);

        result.Select(t => t.Question).Should().Equal("q1", "q2");
    }

    [Fact]
    public void Trim_WithManyTurns_KeepsNewestSix()
    {
        var turns = Enumerable.Range(1, 10).Select(i => ($"q{i}", $"a{i}"));

        var result = HistoryTrimmer.Trim(turns);

        result.Should().HaveCount(6);
        result.First().Question.Should().Be("q5");
        result.Last().Question.Should().Be("q10");
    }

    [Fact]
    public void Trim_WithOversizedHistory_KeepsNewestWithinBudget()
    {
        var turns = new[]
        {
            ("old", new string('x', 3995)),
            ("new", "short"),
        };

        var result = HistoryTrimmer.Trim(turns, maxTurns: 6, maxChars: 4000);

        result.Select(t => t.Question).Should().Equal("new");
    }

    [Fact]
    public void Trim_WithSingleOversizedTurn_KeepsItAnyway()
    {
        var turns = new[] { ("only", new string('x', 9000)) };

        var result = HistoryTrimmer.Trim(turns, maxTurns: 6, maxChars: 4000);

        result.Should().ContainSingle();
    }

    [Fact]
    public void Trim_WithEmptyHistory_ReturnsEmpty()
    {
        HistoryTrimmer.Trim([]).Should().BeEmpty();
    }
}