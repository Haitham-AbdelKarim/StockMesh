using Application.Common.Models;
using Application.DTOs.Inventory;
using Application.Features.Inventory.Commands.AddInventoryBatch;
using Application.Features.Inventory.Commands.ToggleBatchSharing;
using Application.Features.Inventory.Commands.UpdateInventoryBatch;
using Application.Features.Inventory.Queries.GetBatchById;
using Application.Features.Inventory.Queries.GetInventoryBatches;
using Application.Features.Inventory.Queries.GetProductStock;
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers.v1;

[ApiController]
[ApiVersion(1.0)]
[Authorize]
[Route("api/v{version:apiVersion}/inventory")]
public class InventoryController : ControllerBase
{
    private readonly IMediator _mediator;

    public InventoryController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("batches")]
    public async Task<ActionResult<PaginatedList<InventoryBatchResponse>>> GetBatches(
        [FromQuery] Guid? productId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var query = new GetInventoryBatchesQuery(productId, page, pageSize);

        return this.FromResult(await _mediator.Send(query, cancellationToken));
    }

    [HttpGet("batches/{batchId:guid}")]
    public async Task<ActionResult<InventoryBatchResponse>> GetBatchById(
        Guid batchId,
        CancellationToken cancellationToken)
    {
        return this.FromResult(await _mediator.Send(new GetBatchByIdQuery(batchId), cancellationToken));
    }

    [HttpPost("batches")]
    public async Task<ActionResult<InventoryBatchResponse>> AddBatch(
        [FromBody] AddInventoryBatchCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);

        return this.FromResult(result, StatusCodes.Status201Created);
    }

    [HttpPatch("batches/{batchId:guid}")]
    public async Task<ActionResult<InventoryBatchResponse>> UpdateBatch(
        Guid batchId,
        [FromBody] UpdateInventoryBatchCommand command,
        CancellationToken cancellationToken)
    {
        return this.FromResult(
            await _mediator.Send(command with { BatchId = batchId }, cancellationToken));
    }

    [HttpPatch("batches/{batchId:guid}/sharing")]
    public async Task<ActionResult<InventoryBatchResponse>> ToggleSharing(
        Guid batchId,
        [FromBody] ToggleBatchSharingCommand command,
        CancellationToken cancellationToken)
    {
        return this.FromResult(
            await _mediator.Send(command with { BatchId = batchId }, cancellationToken));
    }

    [HttpGet("stock")]
    public async Task<ActionResult<PaginatedList<ProductStockSummaryResponse>>> GetStock(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        return this.FromResult(await _mediator.Send(new GetProductStockQuery(page, pageSize), cancellationToken));
    }
}