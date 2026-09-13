using System.Security.Cryptography;
using System.Text;
using Application.DTOs.AgentLogs;
using Application.Features.AgentLogs.Commands.RecordAgentLog;
using Asp.Versioning;
using Infrastructure.ExternalServices;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers.v1;

[ApiController]
[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/agent-logs")]
public class AgentLogsController : ControllerBase
{
    private const string InternalKeyHeader = "X-Agent-Internal-Key";

    private readonly IMediator _mediator;
    private readonly AgentLogSettings _settings;

    public AgentLogsController(IMediator mediator, AgentLogSettings settings)
    {
        _mediator = mediator;
        _settings = settings;
    }

    [HttpPost]
    public async Task<ActionResult<AgentLogResponse>> Record(
        [FromBody] RecordAgentLogCommand command,
        CancellationToken cancellationToken)
    {
        if (!IsAuthorized(Request.Headers[InternalKeyHeader].ToString()))
        {
            return Unauthorized(new ProblemDetails
            {
                Title = "Unauthorized",
                Status = StatusCodes.Status401Unauthorized,
                Detail = "Missing or invalid internal key.",
            });
        }

        return this.FromResult(
            await _mediator.Send(command, cancellationToken),
            StatusCodes.Status201Created);
    }

    private bool IsAuthorized(string providedKey)
    {
        var expected = Encoding.UTF8.GetBytes(_settings.InternalKey);
        var provided = Encoding.UTF8.GetBytes(providedKey);

        return provided.Length > 0
            && expected.Length == provided.Length
            && CryptographicOperations.FixedTimeEquals(expected, provided);
    }
}