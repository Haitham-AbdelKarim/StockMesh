using Application.Abstractions.Repositories;
using Application.Abstractions.Services;
using Application.Common.Models;
using Application.DTOs.Inventory;
using Domain.Entities;
using MediatR;

namespace Application.Features.Inventory.Commands.AddInventoryBatch;

public sealed class AddInventoryBatchCommandHandler :
    IRequestHandler<AddInventoryBatchCommand, Result<InventoryBatchResponse>>
{
    private readonly ICurrentUser _currentUser;
    private readonly IInventoryBatchRepository _inventoryBatchRepository;
    private readonly IProductRepository _productRepository;

    public AddInventoryBatchCommandHandler(
        ICurrentUser currentUser,
        IInventoryBatchRepository inventoryBatchRepository,
        IProductRepository productRepository)
    {
        _currentUser = currentUser;
        _inventoryBatchRepository = inventoryBatchRepository;
        _productRepository = productRepository;
    }

    public async Task<Result<InventoryBatchResponse>> Handle(
        AddInventoryBatchCommand command,
        CancellationToken cancellationToken)
    {
        var product = await _productRepository.GetByIdAsync(command.ProductId, cancellationToken);

        if (product is null)
        {
            return Result<InventoryBatchResponse>.BadRequest("Product not found.");
        }

        if (product.VerticalCategory != _currentUser.VerticalCategory)
        {
            return Result<InventoryBatchResponse>.BadRequest(
                "Product does not belong to your vertical.");
        }

        var previousBatch = await _inventoryBatchRepository.GetLatestByProductAsync(
            _currentUser.StoreId,
            command.ProductId,
            cancellationToken);

        var unitSalePrice = command.UnitSalePrice
            ?? (previousBatch is null ? command.UnitCost : previousBatch.UnitSalePrice);

        var batch = new InventoryBatch(
            _currentUser.StoreId,
            command.ProductId,
            command.Quantity,
            command.UnitCost,
            unitSalePrice,
            expiryDate: command.ExpiryDate);

        await _inventoryBatchRepository.AddAsync(batch, cancellationToken);
        await _inventoryBatchRepository.SaveChangesAsync(cancellationToken);

        return Result<InventoryBatchResponse>.Success(
            InventoryBatchMapper.ToResponse(batch, product.Name));
    }
}