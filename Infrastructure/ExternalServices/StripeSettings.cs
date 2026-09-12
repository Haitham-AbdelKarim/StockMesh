namespace Infrastructure.ExternalServices;

public sealed class StripeSettings
{
    public const string SectionName = "Stripe";

    public string SecretKey { get; set; } = string.Empty;

    public string WebhookSecret { get; set; } = string.Empty;

    public string Currency { get; set; } = "usd";

    public string SuccessUrl { get; set; } = string.Empty;

    public string CancelUrl { get; set; } = string.Empty;

    public string ConnectRefreshUrl { get; set; } = string.Empty;

    public string ConnectReturnUrl { get; set; } = string.Empty;

    public string DefaultCountry { get; set; } = "US";

    public void Validate(string sectionPath = SectionName)
    {
        if (string.IsNullOrWhiteSpace(SecretKey))
        {
            throw new InvalidOperationException(
                $"Missing '{sectionPath}:SecretKey'. Provide the Stripe test-mode secret key (Stripe__SecretKey).");
        }

        if (string.IsNullOrWhiteSpace(WebhookSecret))
        {
            throw new InvalidOperationException(
                $"Missing '{sectionPath}:WebhookSecret'. Provide the Stripe webhook signing secret (Stripe__WebhookSecret).");
        }

        foreach (var (name, value) in new[]
                 {
                     (nameof(SuccessUrl), SuccessUrl),
                     (nameof(CancelUrl), CancelUrl),
                     (nameof(ConnectRefreshUrl), ConnectRefreshUrl),
                     (nameof(ConnectReturnUrl), ConnectReturnUrl),
                 })
        {
            if (!Uri.TryCreate(value, UriKind.Absolute, out _))
            {
                throw new InvalidOperationException(
                    $"Missing or invalid '{sectionPath}:{name}'. Provide an absolute URL.");
            }
        }
    }
}