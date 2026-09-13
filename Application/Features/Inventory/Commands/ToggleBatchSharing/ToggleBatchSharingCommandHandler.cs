using Application.Abstractions.Repositories;
using Application.Abstractions.Services;
using Application.Common.Models;
using Application.DTOs.Inventory;
using Domain.Entities;
using MediatR;

namespace Application.Features.Inventory.Commands.ToggleBatchSharing;

public sealed class ToggleBatchSharingCommandHandler :
    IRequestHandler<ToggleBatchSharingCommand, Result<InventoryBatchResponse>>
{
    private readonly ICurrentUser _currentUser;
    private readonly IInventoryBatchRepository _inventoryBatchRepository;
    private readonly IProductRepository _productRepository;
    private readonly IAuditLogRepository _auditLogRepository;

    public ToggleBatchSharingCommandHandler(
        ICurrentUser currentUser,
        IInventoryBatchRepository inventoryBatchRepository,
        IProductRepository productRepository,
        IAuditLogRepository auditLogRepository)
    {
        _currentUser = currentUser;
        _inventoryBatchRepository = inventoryBatchRepository;
        _productRepository = productRepository;
        _auditLogRepository = auditLogRepository;
    }

    public async Task<Result<InventoryBatchResponse>> Handle(
        ToggleBatchSharingCommand command,
        CancellationToken cancellationToken)
    {
        var batch = await _inventoryBatchRepository.GetByIdAsync(command.BatchId, cancellationToken);

        if (batch is null || batch.StoreId != _currentUser.StoreId)
        {
            return Result<InventoryBatchResponse>.NotFound("Inventory batch not found.");
        }

        string? action = null;

        if (command.IsShared)
        {
            if (command.SharedQuantity > batch.QuantityRemaining)
            {
                return Result<InventoryBatchResponse>.BadRequest(
                    "Cannot share more units than remain in the batch.");
            }

            batch.MarkAsShared(command.SharedQuantity);
            action = "batch.shared";
        }
        else if (command.SharedQuantity > 0)
        {
            if (command.SharedQuantity > batch.SharedQuantity)
            {
                return Result<InventoryBatchResponse>.BadRequest(
                    "Cannot unshare more units than are currently shared.");
            }

            batch.Unshare(command.SharedQuantity);
            action = "batch.unshared";
        }
        else if (batch.SharedQuantity > 0)
        {
            batch.Unshare(batch.SharedQuantity);
            action = "batch.unshared";
        }

        if (action is not null)
        {
            await _auditLogRepository.AddAsync(new AuditLog(
                nameof(InventoryBatch),
                batch.Id,
                action,
                _currentUser.StoreId), cancellationToken);
        }

        var product = await _productRepository.GetByIdAsync(batch.ProductId, cancellationToken);

        await _inventoryBatchRepository.SaveChangesAsync(cancellationToken);

        return Result<InventoryBatchResponse>.Success(
            InventoryBatchMapper.ToResponse(batch, product?.Name ?? string.Empty));
    }
}