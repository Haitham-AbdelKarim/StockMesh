namespace Application.Abstractions.Options;

public sealed class MarketSignalOptions
{
    public const string SectionName = "MarketSignalSettings";

    public int SweepIntervalHours { get; set; } = 24;
}