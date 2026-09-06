using Application.Common.Models;
using Application.DTOs.Network;
using Application.Features.Network.Queries.GetNetworkListings;
using Asp.Versioning;
using Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers.v1;

[ApiController]
[ApiVersion(1.0)]
[Authorize]
[Route("api/v{version:apiVersion}/network")]
public class NetworkController : ControllerBase
{
    private readonly IMediator _mediator;

    public NetworkController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("listings")]
    public async Task<ActionResult<PaginatedList<NetworkListingResponse>>> GetListings(
        [FromQuery] VerticalCategory? category,
        [FromQuery] double? maxDistanceKm,
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var query = new GetNetworkListingsQuery(category, maxDistanceKm, search, page, pageSize);

        return this.FromResult(await _mediator.Send(query, cancellationToken));
    }
}