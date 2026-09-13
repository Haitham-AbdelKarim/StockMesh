namespace Infrastructure.ExternalServices;

public sealed class ForecastingSettings
{
    public const string SectionName = "Forecasting";

    public string BaseUrl { get; set; } = string.Empty;

    public int TimeoutSeconds { get; set; } = 30;

    public void Validate(string sectionPath = SectionName)
    {
        if (!Uri.TryCreate(BaseUrl, UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            throw new InvalidOperationException(
                $"Missing or invalid '{sectionPath}:BaseUrl'. Provide the forecasting-service address (see docker-compose.yml).");
        }

        if (TimeoutSeconds is < 1 or > 300)
        {
            throw new InvalidOperationException($"'{sectionPath}:TimeoutSeconds' must be between 1 and 300.");
        }
    }
}