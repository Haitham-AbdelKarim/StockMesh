using Api.Auth;
using Application.DTOs.Stores;
using Application.Features.Stores.Commands.UpdateStoreProfile;
using Application.Features.Stores.Queries.GetStoreProfile;
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers.v1;

[ApiController]
[ApiVersion(1.0)]
[Authorize]
[Route("api/v{version:apiVersion}/stores")]
public class StoresController : ControllerBase
{
    private readonly IMediator _mediator;

    public StoresController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("me")]
    public async Task<ActionResult<StoreProfileResponse>> GetProfile(
        CancellationToken cancellationToken)
    {
        return this.FromResult(await _mediator.Send(new GetStoreProfileQuery(), cancellationToken));
    }

    [HttpPatch("me")]
    [Authorize(Policy = Policies.OwnerOnly)]
    public async Task<ActionResult<StoreProfileResponse>> UpdateProfile(
        [FromBody] UpdateStoreProfileCommand command,
        CancellationToken cancellationToken)
    {
        return this.FromResult(await _mediator.Send(command, cancellationToken));
    }
}