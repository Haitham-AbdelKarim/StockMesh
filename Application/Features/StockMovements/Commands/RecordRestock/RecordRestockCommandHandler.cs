using Application.Abstractions.Repositories;
using Application.Abstractions.Services;
using Application.Common.Models;
using Application.DTOs.StockMovements;
using Domain.Entities;
using Domain.Enums;
using MediatR;

namespace Application.Features.StockMovements.Commands.RecordRestock;

public sealed class RecordRestockCommandHandler :
    IRequestHandler<RecordRestockCommand, Result<RestockResponse>>
{
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly IInventoryBatchRepository _inventoryBatchRepository;
    private readonly IProductRepository _productRepository;
    private readonly IStockMovementRepository _stockMovementRepository;

    public RecordRestockCommandHandler(
        ICurrentUser currentUser,
        IDateTimeProvider clock,
        IInventoryBatchRepository inventoryBatchRepository,
        IProductRepository productRepository,
        IStockMovementRepository stockMovementRepository)
    {
        _currentUser = currentUser;
        _clock = clock;
        _inventoryBatchRepository = inventoryBatchRepository;
        _productRepository = productRepository;
        _stockMovementRepository = stockMovementRepository;
    }

    public async Task<Result<RestockResponse>> Handle(
        RecordRestockCommand command,
        CancellationToken cancellationToken)
    {
        var product = await _productRepository.GetByIdAsync(command.ProductId, cancellationToken);

        if (product is null)
        {
            return Result<RestockResponse>.BadRequest("Product not found.");
        }

        if (product.VerticalCategory != _currentUser.VerticalCategory)
        {
            return Result<RestockResponse>.BadRequest(
                "Product does not belong to your vertical.");
        }

        var previousBatch = await _inventoryBatchRepository.GetLatestByProductAsync(
            _currentUser.StoreId,
            command.ProductId,
            cancellationToken);

        var unitSalePrice = previousBatch is null ? command.UnitCost : previousBatch.UnitSalePrice;

        var batch = new InventoryBatch(
            _currentUser.StoreId,
            command.ProductId,
            command.Quantity,
            command.UnitCost,
            unitSalePrice,
            expiryDate: command.ExpiryDate);

        await _inventoryBatchRepository.AddAsync(batch, cancellationToken);

        var movement = new StockMovement(
            _currentUser.StoreId,
            batch.Id,
            MovementType.Restock,
            command.Quantity,
            _clock.UtcNow,
            unitCost: command.UnitCost,
            supplierName: command.SupplierName);

        await _stockMovementRepository.AddAsync(movement, cancellationToken);
        await _stockMovementRepository.SaveChangesAsync(cancellationToken);

        return Result<RestockResponse>.Success(new RestockResponse(
            batch.Id,
            command.ProductId,
            command.Quantity,
            command.UnitCost,
            unitSalePrice));
    }
}