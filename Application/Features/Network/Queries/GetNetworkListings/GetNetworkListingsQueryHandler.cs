using Application.Abstractions.Repositories;
using Application.Abstractions.Services;
using Application.Common.Geo;
using Application.Common.Models;
using Application.DTOs.Network;
using Domain.Entities;
using MediatR;

namespace Application.Features.Network.Queries.GetNetworkListings;

public sealed class GetNetworkListingsQueryHandler :
    IRequestHandler<GetNetworkListingsQuery, Result<PaginatedList<NetworkListingResponse>>>
{
    private readonly ICurrentUser _currentUser;
    private readonly IStoreRepository _storeRepository;
    private readonly IInventoryBatchRepository _inventoryBatchRepository;
    private readonly IProductRepository _productRepository;

    public GetNetworkListingsQueryHandler(
        ICurrentUser currentUser,
        IStoreRepository storeRepository,
        IInventoryBatchRepository inventoryBatchRepository,
        IProductRepository productRepository)
    {
        _currentUser = currentUser;
        _storeRepository = storeRepository;
        _inventoryBatchRepository = inventoryBatchRepository;
        _productRepository = productRepository;
    }

    public async Task<Result<PaginatedList<NetworkListingResponse>>> Handle(
        GetNetworkListingsQuery query,
        CancellationToken cancellationToken)
    {
        var currentStore = await _storeRepository.GetByIdAsync(_currentUser.StoreId, cancellationToken);

        if (currentStore is null)
        {
            return Result<PaginatedList<NetworkListingResponse>>.NotFound("Store not found.");
        }

        var verticalCategory = query.Category ?? currentStore.VerticalCategory;
        var maxDistanceKm = query.MaxDistanceKm ?? currentStore.MaxSearchRadiusKm;

        var candidateStores = (await _storeRepository.GetByVerticalAsync(verticalCategory, cancellationToken))
            .Where(s => s.Id != currentStore.Id)
            .Select(s => new StoreCandidate(s, DistanceCalculator.HaversineKm(
                currentStore.Latitude,
                currentStore.Longitude,
                s.Latitude,
                s.Longitude)))
            .Where(x => x.DistanceKm <= maxDistanceKm)
            .OrderBy(x => x.DistanceKm)
            .ToList();

        if (candidateStores.Count == 0)
        {
            return Result<PaginatedList<NetworkListingResponse>>.Success(
                PaginatedList<NetworkListingResponse>.Create([], 0, query.Page, query.PageSize));
        }

        var storesById = candidateStores.ToDictionary(x => x.Store.Id, x => x.Store);
        var distancesKm = candidateStores.ToDictionary(x => x.Store.Id, x => x.DistanceKm);

        var batches = await _inventoryBatchRepository.GetSharedByStoresAsync(
            storesById.Keys.ToList(),
            cancellationToken);

        var products = await _productRepository.GetByIdsAsync(
            batches.Select(b => b.ProductId).Distinct().ToList(),
            cancellationToken);
        var productById = products.ToDictionary(p => p.Id);

        var searchTerm = string.IsNullOrWhiteSpace(query.Search)
            ? null
            : query.Search.Trim().ToLower();

        var candidates = new List<BatchCandidate>();

        foreach (var batch in batches)
        {
            if (!productById.TryGetValue(batch.ProductId, out var product))
            {
                continue;
            }

            if (searchTerm is not null
                && !product.Name.ToLower().Contains(searchTerm)
                && (product.Brand is null || !product.Brand.ToLower().Contains(searchTerm)))
            {
                continue;
            }

            candidates.Add(new BatchCandidate(
                batch,
                product.Name,
                Math.Round(distancesKm[batch.StoreId], 2)));
        }

        var ordered = candidates
            .OrderBy(x => x.DistanceKm)
            .ThenBy(x => x.Batch.ExpiryDate ?? DateTime.MaxValue)
            .ThenBy(x => x.Batch.Id)
            .ToList();

        var items = ordered
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(x => new NetworkListingResponse(
                x.Batch.Id,
                x.Batch.StoreId,
                x.Batch.ProductId,
                x.ProductName,
                storesById[x.Batch.StoreId].Name,
                x.DistanceKm,
                x.Batch.SharedQuantity,
                x.Batch.UnitSalePrice,
                x.Batch.ExpiryDate))
            .ToList();

        var paginated = PaginatedList<NetworkListingResponse>.Create(
            items,
            ordered.Count,
            query.Page,
            query.PageSize);

        return Result<PaginatedList<NetworkListingResponse>>.Success(paginated);
    }

    private sealed record StoreCandidate(Store Store, double DistanceKm);

    private sealed record BatchCandidate(InventoryBatch Batch, string ProductName, double DistanceKm);
}