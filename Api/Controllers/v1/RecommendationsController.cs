using Api.Auth;
using Application.Common.Models;
using Application.DTOs.Recommendations;
using Application.Features.Recommendations.Commands.RunRecommendations;
using Application.Features.Recommendations.Queries.GetRecommendations;
using Asp.Versioning;
using Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers.v1;

[ApiController]
[ApiVersion(1.0)]
[Authorize]
[Route("api/v{version:apiVersion}/recommendations")]
public class RecommendationsController : ControllerBase
{
    private readonly IMediator _mediator;

    public RecommendationsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<ActionResult<PaginatedList<RecommendationResponse>>> GetRecommendations(
        [FromQuery] RecommendedAction? recommendedAction,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        return this.FromResult(
            await _mediator.Send(
                new GetRecommendationsQuery(recommendedAction, page, pageSize),
                cancellationToken));
    }

    [HttpPost("run")]
    [Authorize(Policy = Policies.OwnerOnly)]
    public async Task<ActionResult<RunSummaryResponse>> Run(
        CancellationToken cancellationToken)
    {
        return this.FromResult(
            await _mediator.Send(new RunRecommendationsCommand(), cancellationToken));
    }
}