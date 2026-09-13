namespace Infrastructure.ExternalServices;

public sealed class AgentLogSettings
{
    public const string SectionName = "AgentLog";

    public string InternalKey { get; set; } = string.Empty;

    public void Validate(string sectionPath = SectionName)
    {
        if (string.IsNullOrWhiteSpace(InternalKey))
        {
            throw new InvalidOperationException(
                $"Missing '{sectionPath}:InternalKey'. Provide the agent-service callback secret (AgentLog__InternalKey).");
        }
    }
}