namespace Application.Abstractions.Options;

public sealed class ReservationOptions
{
    public const string SectionName = "ReservationSettings";

    public int HoldMinutes { get; set; } = 15;

    public int ExpirySweepIntervalSeconds { get; set; } = 60;
}