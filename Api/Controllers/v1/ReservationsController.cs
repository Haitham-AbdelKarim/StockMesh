using Application.DTOs.Reservations;
using Application.Features.Reservations.Commands.ReserveStock;
using Application.Features.Reservations.Commands.ResolveReservation;
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers.v1;

[ApiController]
[ApiVersion(1.0)]
[Authorize]
[Route("api/v{version:apiVersion}/reservations")]
public class ReservationsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ReservationsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    public async Task<ActionResult<ReservationResponse>> Reserve(
        [FromBody] ReserveStockCommand command,
        CancellationToken cancellationToken)
    {
        return this.FromResult(
            await _mediator.Send(command, cancellationToken),
            StatusCodes.Status201Created);
    }

    [HttpPatch("{reservationId:guid}")]
    public async Task<ActionResult<ReservationResponse>> Resolve(
        Guid reservationId,
        [FromBody] ResolveReservationCommand command,
        CancellationToken cancellationToken)
    {
        return this.FromResult(
            await _mediator.Send(command with { ReservationId = reservationId }, cancellationToken));
    }
}