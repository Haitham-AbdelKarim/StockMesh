using Application.Common.Models;
using Application.DTOs.Inventory;
using MediatR;

namespace Application.Features.Inventory.Commands.ToggleBatchSharing;

public sealed record ToggleBatchSharingCommand(
    Guid BatchId,
    bool IsShared,
    int SharedQuantity) : IRequest<Result<InventoryBatchResponse>>;