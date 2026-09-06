using Application.Abstractions.Repositories;
using Application.Abstractions.Services;
using Application.Common.Models;
using Application.DTOs.Inventory;
using MediatR;

namespace Application.Features.Inventory.Queries.GetProductStock;

public sealed class GetProductStockQueryHandler :
    IRequestHandler<GetProductStockQuery, Result<PaginatedList<ProductStockSummaryResponse>>>
{
    private readonly ICurrentUser _currentUser;
    private readonly IInventoryBatchRepository _inventoryBatchRepository;
    private readonly IProductRepository _productRepository;

    public GetProductStockQueryHandler(
        ICurrentUser currentUser,
        IInventoryBatchRepository inventoryBatchRepository,
        IProductRepository productRepository)
    {
        _currentUser = currentUser;
        _inventoryBatchRepository = inventoryBatchRepository;
        _productRepository = productRepository;
    }

    public async Task<Result<PaginatedList<ProductStockSummaryResponse>>> Handle(
        GetProductStockQuery query,
        CancellationToken cancellationToken)
    {
        var summaries = await _inventoryBatchRepository.GetProductStockSummariesAsync(
            _currentUser.StoreId,
            cancellationToken);

        var productIds = summaries.Select(s => s.ProductId).Distinct().ToList();
        var products = await _productRepository.GetByIdsAsync(productIds, cancellationToken);
        var productNames = products.ToDictionary(p => p.Id, p => p.Name);

        var items = summaries
            .Select(s => new ProductStockSummaryResponse(
                s.ProductId,
                productNames.GetValueOrDefault(s.ProductId) ?? string.Empty,
                s.TotalQuantityRemaining,
                s.TotalSharedQuantity,
                s.BatchCount))
            .OrderByDescending(s => s.TotalQuantityRemaining)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToList();

        var paginated = PaginatedList<ProductStockSummaryResponse>.Create(
            items,
            summaries.Count,
            query.Page,
            query.PageSize);

        return Result<PaginatedList<ProductStockSummaryResponse>>.Success(paginated);
    }
}