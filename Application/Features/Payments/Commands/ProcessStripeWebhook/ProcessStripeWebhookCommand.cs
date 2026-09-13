using Application.Common.Models;
using MediatR;

namespace Application.Features.Payments.Commands.ProcessStripeWebhook;

public sealed record ProcessStripeWebhookCommand(
    string Payload,
    string SignatureHeader) : IRequest<Result<WebhookOutcome>>;