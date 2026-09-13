using Application.Abstractions.Services;
using Application.DTOs.Assistant;
using Application.Features.Assistant.Commands.AskAssistant;
using Application.Features.Assistant.Queries.GetConversationById;
using Application.Features.Assistant.Queries.GetConversations;
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace Api.Controllers.v1;

[ApiController]
[ApiVersion(1.0)]
[Authorize]
[Route("api/v{version:apiVersion}/assistant")]
public class AssistantController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IAssistantClient _assistantClient;

    public AssistantController(
        IMediator mediator,
        IAssistantClient assistantClient)
    {
        _mediator = mediator;
        _assistantClient = assistantClient;
    }

    [HttpPost("ask")]
    public async Task Ask(
        [FromBody] AskAssistantCommand command,
        CancellationToken cancellationToken)
    {
        var ticket = await _mediator.Send(command, cancellationToken);

        if (!ticket.IsSuccess)
        {
            var failure = ((IConvertToActionResult)this.FromResult(ticket)).Convert();
            await failure.ExecuteResultAsync(ControllerContext);
            return;
        }

        var bearerToken = Request.Headers.Authorization.ToString();

        try
        {
            Response.ContentType = "text/event-stream";
            Response.Headers.CacheControl = "no-cache";
            Response.Headers["X-Accel-Buffering"] = "no";
            Response.Headers.Connection = "keep-alive";

            await _assistantClient.StreamAnswerAsync(
                ticket.Value!,
                bearerToken,
                Response.Body,
                cancellationToken);
        }
        catch when (!cancellationToken.IsCancellationRequested && !Response.HasStarted)
        {
            Response.StatusCode = StatusCodes.Status502BadGateway;
            Response.ContentType = "application/problem+json";
            await Response.WriteAsJsonAsync(
                new ProblemDetails
                {
                    Title = "Bad gateway",
                    Status = StatusCodes.Status502BadGateway,
                    Detail = "The assistant service failed to respond.",
                },
                cancellationToken);
        }
    }

    [HttpGet("conversations")]
    public async Task<ActionResult<IReadOnlyList<ConversationResponse>>> GetConversations(
        CancellationToken cancellationToken)
    {
        return this.FromResult(await _mediator.Send(new GetConversationsQuery(), cancellationToken));
    }

    [HttpGet("conversations/{conversationId:guid}")]
    public async Task<ActionResult<ConversationDetailResponse>> GetConversationById(
        Guid conversationId,
        CancellationToken cancellationToken)
    {
        return this.FromResult(
            await _mediator.Send(new GetConversationByIdQuery(conversationId), cancellationToken));
    }
}