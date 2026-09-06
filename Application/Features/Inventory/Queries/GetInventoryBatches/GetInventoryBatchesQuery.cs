using Application.Common.Models;
using Application.DTOs.Inventory;
using MediatR;

namespace Application.Features.Inventory.Queries.GetInventoryBatches;

public sealed record GetInventoryBatchesQuery(
    Guid? ProductId = null,
    int Page = 1,
    int PageSize = 20) : IRequest<Result<PaginatedList<InventoryBatchResponse>>>;