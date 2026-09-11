namespace Application.Abstractions.Options;

public sealed class RecommendationOptions
{
    public const string SectionName = "RecommendationSettings";

    public int SweepIntervalHours { get; set; } = 24;
}