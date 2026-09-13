using Application.Common.Models;
using Application.DTOs.Dashboard;
using Application.Features.Dashboard.Queries.GetDashboardSummary;
using Application.Features.Dashboard.Queries.GetLowStock;
using Application.Features.Dashboard.Queries.GetNetworkSummary;
using Application.Features.Dashboard.Queries.GetSalesTrend;
using Application.Features.Dashboard.Queries.GetSlowMovers;
using Application.Features.Dashboard.Queries.GetTopSellers;
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers.v1;

[ApiController]
[ApiVersion(1.0)]
[Authorize]
[Route("api/v{version:apiVersion}/dashboard")]
public class DashboardController : ControllerBase
{
    private readonly IMediator _mediator;

    public DashboardController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("summary")]
    public async Task<ActionResult<DashboardSummaryResponse>> GetSummary(
        [FromQuery] GetDashboardSummaryQuery query,
        CancellationToken cancellationToken)
    {
        return this.FromResult(
            await _mediator.Send(query, cancellationToken));
    }

    [HttpGet("sales-trend")]
    public async Task<ActionResult<IReadOnlyList<SalesTrendPointResponse>>> GetSalesTrend(
        [FromQuery] GetSalesTrendQuery query,
        CancellationToken cancellationToken)
    {
        return this.FromResult(
            await _mediator.Send(query, cancellationToken));
    }

    [HttpGet("top-sellers")]
    public async Task<ActionResult<IReadOnlyList<TopSellerResponse>>> GetTopSellers(
        [FromQuery] GetTopSellersQuery query,
        CancellationToken cancellationToken)
    {
        return this.FromResult(
            await _mediator.Send(query, cancellationToken));
    }

    [HttpGet("slow-movers")]
    public async Task<ActionResult<IReadOnlyList<SlowMoverResponse>>> GetSlowMovers(
        [FromQuery] GetSlowMoversQuery query,
        CancellationToken cancellationToken)
    {
        return this.FromResult(
            await _mediator.Send(query, cancellationToken));
    }

    [HttpGet("network")]
    public async Task<ActionResult<NetworkSummaryResponse>> GetNetworkSummary(
        [FromQuery] GetNetworkSummaryQuery query,
        CancellationToken cancellationToken)
    {
        return this.FromResult(
            await _mediator.Send(query, cancellationToken));
    }

    [HttpGet("low-stock")]
    public async Task<ActionResult<IReadOnlyList<LowStockItemResponse>>> GetLowStock(
        [FromQuery] GetLowStockQuery query,
        CancellationToken cancellationToken)
    {
        return this.FromResult(
            await _mediator.Send(query, cancellationToken));
    }
}