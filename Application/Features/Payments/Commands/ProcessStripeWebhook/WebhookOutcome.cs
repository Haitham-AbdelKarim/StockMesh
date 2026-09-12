namespace Application.Features.Payments.Commands.ProcessStripeWebhook;

public enum WebhookOutcome
{
    Ignored,
    Paid,
    Failed
}