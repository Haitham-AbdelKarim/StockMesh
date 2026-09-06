using Application.Common.Models;
using Application.DTOs.Inventory;
using MediatR;

namespace Application.Features.Inventory.Commands.UpdateInventoryBatch;

public sealed record UpdateInventoryBatchCommand(
    Guid BatchId,
    decimal? UnitSalePrice = null,
    int? ReorderPoint = null,
    int? LeadTimeDays = null,
    DateTime? ExpiryDate = null) : IRequest<Result<InventoryBatchResponse>>;