using Application.Common.Models;
using Application.DTOs.Inventory;
using MediatR;

namespace Application.Features.Inventory.Commands.AddInventoryBatch;

public sealed record AddInventoryBatchCommand(
    Guid ProductId,
    int Quantity,
    decimal UnitCost,
    decimal? UnitSalePrice = null,
    DateTime? ExpiryDate = null) : IRequest<Result<InventoryBatchResponse>>;