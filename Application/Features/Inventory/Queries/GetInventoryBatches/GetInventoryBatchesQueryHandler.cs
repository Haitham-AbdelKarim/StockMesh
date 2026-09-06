using Application.Abstractions.Repositories;
using Application.Abstractions.Services;
using Application.Common.Models;
using Application.DTOs.Inventory;
using Domain.Entities;
using MediatR;

namespace Application.Features.Inventory.Queries.GetInventoryBatches;

public sealed class GetInventoryBatchesQueryHandler :
    IRequestHandler<GetInventoryBatchesQuery, Result<PaginatedList<InventoryBatchResponse>>>
{
    private readonly ICurrentUser _currentUser;
    private readonly IInventoryBatchRepository _inventoryBatchRepository;
    private readonly IProductRepository _productRepository;

    public GetInventoryBatchesQueryHandler(
        ICurrentUser currentUser,
        IInventoryBatchRepository inventoryBatchRepository,
        IProductRepository productRepository)
    {
        _currentUser = currentUser;
        _inventoryBatchRepository = inventoryBatchRepository;
        _productRepository = productRepository;
    }

    public async Task<Result<PaginatedList<InventoryBatchResponse>>> Handle(
        GetInventoryBatchesQuery query,
        CancellationToken cancellationToken)
    {
        var (items, totalCount) = await _inventoryBatchRepository.GetBatchesAsync(
            _currentUser.StoreId,
            query.ProductId,
            query.Page,
            query.PageSize,
            cancellationToken);

        var productNames = await GetProductNamesAsync(
            items.Select(b => b.ProductId),
            cancellationToken);

        var responseItems = items
            .Select(b => InventoryBatchMapper.ToResponse(
                b,
                productNames.GetValueOrDefault(b.ProductId) ?? string.Empty))
            .ToList();

        var paginated = PaginatedList<InventoryBatchResponse>.Create(
            responseItems,
            totalCount,
            query.Page,
            query.PageSize);

        return Result<PaginatedList<InventoryBatchResponse>>.Success(paginated);
    }

    private async Task<IReadOnlyDictionary<Guid, string>> GetProductNamesAsync(
        IEnumerable<Guid> productIds,
        CancellationToken cancellationToken)
    {
        var products = await _productRepository.GetByIdsAsync(
            productIds.Distinct().ToList(),
            cancellationToken);

        return products.ToDictionary(p => p.Id, p => p.Name);
    }
}