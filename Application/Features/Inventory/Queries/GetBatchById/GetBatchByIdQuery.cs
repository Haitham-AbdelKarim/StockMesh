using Application.Common.Models;
using Application.DTOs.Inventory;
using MediatR;

namespace Application.Features.Inventory.Queries.GetBatchById;

public sealed record GetBatchByIdQuery(Guid BatchId) : IRequest<Result<InventoryBatchResponse>>;