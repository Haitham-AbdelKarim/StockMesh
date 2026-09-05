using Api.Auth;
using Application.DTOs.Auth;
using Application.Features.Auth.Commands.JoinStore;
using Application.Features.Auth.Commands.Login;
using Application.Features.Auth.Commands.Refresh;
using Application.Features.Auth.Commands.RegisterStore;
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers.v1;

[ApiController]
[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/auth")]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;

    public AuthController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("register-store")]
    public async Task<ActionResult<RegisterStoreResponse>> RegisterStore(
        [FromBody] RegisterStoreCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);

        return this.FromResult(result, StatusCodes.Status201Created);
    }

    [HttpPost("login")]
    public async Task<ActionResult<TokenResponse>> Login(
        [FromBody] LoginCommand command,
        CancellationToken cancellationToken)
    {
        return this.FromResult(await _mediator.Send(command, cancellationToken));
    }

    [HttpPost("join-store")]
    [Authorize(Policy = Policies.OwnerOnly)]
    public async Task<ActionResult<JoinStoreResponse>> JoinStore(
        [FromBody] JoinStoreCommand command,
        CancellationToken cancellationToken)
    {
        return this.FromResult(await _mediator.Send(command, cancellationToken), StatusCodes.Status201Created);
    }

    [HttpPost("refresh")]
    public async Task<ActionResult<TokenResponse>> Refresh(
        [FromBody] RefreshCommand command,
        CancellationToken cancellationToken)
    {
        return this.FromResult(await _mediator.Send(command, cancellationToken));
    }
}