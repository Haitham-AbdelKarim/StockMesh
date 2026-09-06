using Application.Common.Models;
using Application.DTOs.StockMovements;
using Application.Features.StockMovements.Commands.RecordRestock;
using Application.Features.StockMovements.Commands.RecordSale;
using Application.Features.StockMovements.Queries.GetStockMovements;
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers.v1;

[ApiController]
[ApiVersion(1.0)]
[Authorize]
[Route("api/v{version:apiVersion}/")]
public class StockController : ControllerBase
{
    private readonly IMediator _mediator;

    public StockController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("sales")]
    public async Task<ActionResult<SaleResponse>> RecordSale(
        [FromBody] RecordSaleCommand command,
        CancellationToken cancellationToken)
    {
        return this.FromResult(
            await _mediator.Send(command, cancellationToken),
            StatusCodes.Status201Created);
    }

    [HttpPost("restocks")]
    public async Task<ActionResult<RestockResponse>> RecordRestock(
        [FromBody] RecordRestockCommand command,
        CancellationToken cancellationToken)
    {
        return this.FromResult(
            await _mediator.Send(command, cancellationToken),
            StatusCodes.Status201Created);
    }

    [HttpGet("stock-movements")]
    public async Task<ActionResult<PaginatedList<StockMovementResponse>>> GetStockMovements(
        [FromQuery] GetStockMovementsQuery query,
        CancellationToken cancellationToken)
    {
        return this.FromResult(
            await _mediator.Send(query, cancellationToken));
    }
}