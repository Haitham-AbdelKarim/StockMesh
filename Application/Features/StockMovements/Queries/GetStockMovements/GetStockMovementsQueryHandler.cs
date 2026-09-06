using Application.Abstractions.Repositories;
using Application.Abstractions.Services;
using Application.Common.Models;
using Application.DTOs.StockMovements;
using Domain.Entities;
using MediatR;

namespace Application.Features.StockMovements.Queries.GetStockMovements;

public sealed class GetStockMovementsQueryHandler :
    IRequestHandler<GetStockMovementsQuery, Result<PaginatedList<StockMovementResponse>>>
{
    private readonly ICurrentUser _currentUser;
    private readonly IStockMovementRepository _stockMovementRepository;
    private readonly IInventoryBatchRepository _inventoryBatchRepository;
    private readonly IProductRepository _productRepository;

    public GetStockMovementsQueryHandler(
        ICurrentUser currentUser,
        IStockMovementRepository stockMovementRepository,
        IInventoryBatchRepository inventoryBatchRepository,
        IProductRepository productRepository)
    {
        _currentUser = currentUser;
        _stockMovementRepository = stockMovementRepository;
        _inventoryBatchRepository = inventoryBatchRepository;
        _productRepository = productRepository;
    }

    public async Task<Result<PaginatedList<StockMovementResponse>>> Handle(
        GetStockMovementsQuery query,
        CancellationToken cancellationToken)
    {
        var (items, totalCount) = await _stockMovementRepository.GetByStoreAsync(
            _currentUser.StoreId,
            query.MovementType,
            query.From,
            query.To,
            query.RelatedStoreId,
            query.Page,
            query.PageSize,
            cancellationToken);

        var productInfo = await GetProductInfoAsync(items, cancellationToken);

        var responseItems = items
            .Select(m => StockMovementMapper.ToResponse(
                m,
                productInfo.GetValueOrDefault(m.BatchId).ProductId,
                productInfo.GetValueOrDefault(m.BatchId).ProductName))
            .ToList();

        var paginated = PaginatedList<StockMovementResponse>.Create(
            responseItems,
            totalCount,
            query.Page,
            query.PageSize);

        return Result<PaginatedList<StockMovementResponse>>.Success(paginated);
    }

    private async Task<IReadOnlyDictionary<Guid, (Guid ProductId, string ProductName)>> GetProductInfoAsync(
        IEnumerable<StockMovement> items,
        CancellationToken cancellationToken)
    {
        var batchIds = items.Select(m => m.BatchId).Distinct().ToList();

        if (batchIds.Count == 0)
        {
            return new Dictionary<Guid, (Guid, string)>();
        }

        var batches = await _inventoryBatchRepository.GetByIdsAsync(batchIds, cancellationToken);

        var products = await _productRepository.GetByIdsAsync(
            batches.Select(b => b.ProductId).Distinct().ToList(),
            cancellationToken);

        var productNames = products.ToDictionary(p => p.Id, p => p.Name);

        return batches.ToDictionary(
            b => b.Id,
            b => (b.ProductId, productNames.GetValueOrDefault(b.ProductId) ?? string.Empty));
    }
}