using Application.Common.Models;
using Application.DTOs.Inventory;
using MediatR;

namespace Application.Features.Inventory.Queries.GetProductStock;

public sealed record GetProductStockQuery(
    int Page = 1,
    int PageSize = 20) : IRequest<Result<PaginatedList<ProductStockSummaryResponse>>>;