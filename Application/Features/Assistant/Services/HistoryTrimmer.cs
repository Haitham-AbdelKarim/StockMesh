using Application.DTOs.Assistant;

namespace Application.Features.Assistant.Services;

public static class HistoryTrimmer
{
    public const int MaxTurns = 6;

    public const int MaxChars = 4000;

    public static IReadOnlyList<AssistantHistoryTurn> Trim(
        IEnumerable<(string Question, string Answer)> turns,
        int maxTurns = MaxTurns,
        int maxChars = MaxChars)
    {
        var recent = turns.Reverse().Take(maxTurns).Reverse().ToList();
        var selected = new List<AssistantHistoryTurn>();
        var chars = 0;

        foreach (var (question, answer) in recent.AsEnumerable().Reverse())
        {
            var cost = question.Length + answer.Length;

            if (selected.Count > 0 && chars + cost > maxChars)
            {
                break;
            }

            selected.Insert(0, new AssistantHistoryTurn(question, answer));
            chars += cost;
        }

        return selected;
    }
}