using Application.Abstractions.Repositories;
using Application.Abstractions.Services;
using Application.Common.Models;
using Application.DTOs.Dashboard;
using MediatR;

namespace Application.Features.Dashboard.Queries.GetLowStock;

public sealed class GetLowStockQueryHandler :
    IRequestHandler<GetLowStockQuery, Result<IReadOnlyList<LowStockItemResponse>>>
{
    private readonly ICurrentUser _currentUser;
    private readonly IInventoryBatchRepository _inventoryBatchRepository;
    private readonly IProductRepository _productRepository;

    public GetLowStockQueryHandler(
        ICurrentUser currentUser,
        IInventoryBatchRepository inventoryBatchRepository,
        IProductRepository productRepository)
    {
        _currentUser = currentUser;
        _inventoryBatchRepository = inventoryBatchRepository;
        _productRepository = productRepository;
    }

    public async Task<Result<IReadOnlyList<LowStockItemResponse>>> Handle(
        GetLowStockQuery query,
        CancellationToken cancellationToken)
    {
        var items = await _inventoryBatchRepository.GetLowStockItemsAsync(
            _currentUser.StoreId,
            cancellationToken);

        var products = await LoadProductNamesAsync(
            items.Select(i => i.ProductId).ToList(),
            cancellationToken);

        var response = items
            .Select(i => new LowStockItemResponse(
                i.ProductId,
                products.GetValueOrDefault(i.ProductId) ?? string.Empty,
                i.TotalQuantityRemaining,
                i.ReorderPoint,
                i.LeadTimeDays))
            .ToList();

        return Result<IReadOnlyList<LowStockItemResponse>>.Success(response);
    }

    private async Task<IReadOnlyDictionary<Guid, string>> LoadProductNamesAsync(
        IReadOnlyCollection<Guid> productIds,
        CancellationToken cancellationToken)
    {
        if (productIds.Count == 0)
        {
            return new Dictionary<Guid, string>();
        }

        var products = await _productRepository.GetByIdsAsync(productIds, cancellationToken);

        return products.ToDictionary(p => p.Id, p => p.Name);
    }
}