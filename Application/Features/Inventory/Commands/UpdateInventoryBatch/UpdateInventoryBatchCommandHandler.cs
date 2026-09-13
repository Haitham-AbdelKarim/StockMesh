using Application.Abstractions.Repositories;
using Application.Abstractions.Services;
using Application.Common.Models;
using Application.DTOs.Inventory;
using Domain.Entities;
using MediatR;

namespace Application.Features.Inventory.Commands.UpdateInventoryBatch;

public sealed class UpdateInventoryBatchCommandHandler :
    IRequestHandler<UpdateInventoryBatchCommand, Result<InventoryBatchResponse>>
{
    private readonly ICurrentUser _currentUser;
    private readonly IInventoryBatchRepository _inventoryBatchRepository;
    private readonly IProductRepository _productRepository;
    private readonly IAuditLogRepository _auditLogRepository;

    public UpdateInventoryBatchCommandHandler(
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
        UpdateInventoryBatchCommand command,
        CancellationToken cancellationToken)
    {
        var batch = await _inventoryBatchRepository.GetByIdAsync(command.BatchId, cancellationToken);

        if (batch is null || batch.StoreId != _currentUser.StoreId)
        {
            return Result<InventoryBatchResponse>.NotFound("Inventory batch not found.");
        }

        var product = await _productRepository.GetByIdAsync(batch.ProductId, cancellationToken);

        batch.UpdateDetails(
            command.UnitSalePrice,
            command.ReorderPoint,
            command.LeadTimeDays,
            command.ExpiryDate);

        await _auditLogRepository.AddAsync(new AuditLog(
            nameof(InventoryBatch),
            batch.Id,
            "batch.updated",
            _currentUser.StoreId), cancellationToken);

        await _inventoryBatchRepository.SaveChangesAsync(cancellationToken);

        return Result<InventoryBatchResponse>.Success(
            InventoryBatchMapper.ToResponse(batch, product?.Name ?? string.Empty));
    }
}