using Application.Abstractions.Repositories;
using Application.Abstractions.Services;
using Application.Common.Models;
using Application.DTOs.Inventory;
using MediatR;

namespace Application.Features.Inventory.Queries.GetBatchById;

public sealed class GetBatchByIdQueryHandler :
    IRequestHandler<GetBatchByIdQuery, Result<InventoryBatchResponse>>
{
    private readonly ICurrentUser _currentUser;
    private readonly IInventoryBatchRepository _inventoryBatchRepository;
    private readonly IProductRepository _productRepository;

    public GetBatchByIdQueryHandler(
        ICurrentUser currentUser,
        IInventoryBatchRepository inventoryBatchRepository,
        IProductRepository productRepository)
    {
        _currentUser = currentUser;
        _inventoryBatchRepository = inventoryBatchRepository;
        _productRepository = productRepository;
    }

    public async Task<Result<InventoryBatchResponse>> Handle(
        GetBatchByIdQuery query,
        CancellationToken cancellationToken)
    {
        var batch = await _inventoryBatchRepository.GetByIdAsync(query.BatchId, cancellationToken);

        if (batch is null || batch.StoreId != _currentUser.StoreId)
        {
            return Result<InventoryBatchResponse>.NotFound("Inventory batch not found.");
        }

        var product = await _productRepository.GetByIdAsync(batch.ProductId, cancellationToken);

        return Result<InventoryBatchResponse>.Success(
            InventoryBatchMapper.ToResponse(batch, product?.Name ?? string.Empty));
    }
}