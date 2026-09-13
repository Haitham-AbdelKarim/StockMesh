namespace Infrastructure.ExternalServices;

public sealed class AssistantSettings
{
    public const string SectionName = "Assistant";

    public string BaseUrl { get; set; } = string.Empty;

    public int TimeoutSeconds { get; set; } = 120;

    public int MaxQuestionsPerMinute { get; set; } = 10;

    public void Validate(string sectionPath = SectionName)
    {
        if (!Uri.TryCreate(BaseUrl, UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            throw new InvalidOperationException(
                $"Missing or invalid '{sectionPath}:BaseUrl'. Provide the agent-service address (see docker-compose.yml).");
        }

        if (TimeoutSeconds is < 10 or > 600)
        {
            throw new InvalidOperationException($"'{sectionPath}:TimeoutSeconds' must be between 10 and 600.");
        }

        if (MaxQuestionsPerMinute is < 1 or > 1000)
        {
            throw new InvalidOperationException($"'{sectionPath}:MaxQuestionsPerMinute' must be between 1 and 1000.");
        }
    }
}