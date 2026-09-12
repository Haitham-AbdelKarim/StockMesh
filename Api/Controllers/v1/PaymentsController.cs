using Api.Auth;
using Application.DTOs.Payments;
using Application.Features.Payments.Commands.CreateConnectOnboarding;
using Application.Features.Payments.Commands.ProcessStripeWebhook;
using Application.Features.Payments.Queries.GetConnectStatus;
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers.v1;

[ApiController]
[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/payments")]
public class PaymentsController : ControllerBase
{
    private readonly IMediator _mediator;

    public PaymentsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("connect/onboard")]
    [Authorize(Policy = Policies.OwnerOnly)]
    public async Task<ActionResult<ConnectOnboardingResponse>> Onboard(
        CancellationToken cancellationToken)
    {
        return this.FromResult(
            await _mediator.Send(new CreateConnectOnboardingCommand(), cancellationToken));
    }

    [HttpGet("connect/status")]
    [Authorize]
    public async Task<ActionResult<ConnectStatusResponse>> GetConnectStatus(
        CancellationToken cancellationToken)
    {
        return this.FromResult(
            await _mediator.Send(new GetConnectStatusQuery(), cancellationToken));
    }

    [HttpPost("webhook")]
    [AllowAnonymous]
    public async Task<ActionResult<WebhookOutcome>> StripeWebhook(
        CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(Request.Body);
        var payload = await reader.ReadToEndAsync(cancellationToken);
        var signature = Request.Headers["Stripe-Signature"].ToString();

        return this.FromResult(
            await _mediator.Send(
                new ProcessStripeWebhookCommand(payload, signature),
                cancellationToken));
    }
}