using Application.Abstractions.Repositories;
using Application.Abstractions.Services;
using Application.Common.Models;
using Application.DTOs.Reservations;
using MediatR;

namespace Application.Features.Reservations.Queries.GetReservations;

public sealed class GetReservationsQueryHandler :
    IRequestHandler<GetReservationsQuery, Result<PaginatedList<ReservationDetailResponse>>>
{
    private readonly ICurrentUser _currentUser;
    private readonly IStockReservationRepository _stockReservationRepository;
    private readonly IInventoryBatchRepository _inventoryBatchRepository;
    private readonly IProductRepository _productRepository;
    private readonly IStoreRepository _storeRepository;

    public GetReservationsQueryHandler(
        ICurrentUser currentUser,
        IStockReservationRepository stockReservationRepository,
        IInventoryBatchRepository inventoryBatchRepository,
        IProductRepository productRepository,
        IStoreRepository storeRepository)
    {
        _currentUser = currentUser;
        _stockReservationRepository = stockReservationRepository;
        _inventoryBatchRepository = inventoryBatchRepository;
        _productRepository = productRepository;
        _storeRepository = storeRepository;
    }

    public async Task<Result<PaginatedList<ReservationDetailResponse>>> Handle(
        GetReservationsQuery query,
        CancellationToken cancellationToken)
    {
        var (items, totalCount) = await _stockReservationRepository.GetForStoreAsync(
            _currentUser.StoreId,
            query.Incoming,
            query.Status,
            query.From,
            query.To,
            query.Page,
            query.PageSize,
            cancellationToken);

        var productInfo = await GetProductInfoAsync(items, cancellationToken);

        var storeNames = await GetStoreNamesAsync(items, cancellationToken);

        var responseItems = items
            .Select(r => new ReservationDetailResponse(
                r.Id,
                r.BatchId,
                productInfo.GetValueOrDefault(r.BatchId).ProductId,
                productInfo.GetValueOrDefault(r.BatchId).ProductName,
                r.RequestingStoreId,
                storeNames.GetValueOrDefault(r.RequestingStoreId) ?? string.Empty,
                r.OwningStoreId,
                storeNames.GetValueOrDefault(r.OwningStoreId) ?? string.Empty,
                r.Quantity,
                r.UnitPrice,
                r.DistanceKm,
                r.DeliveryEta,
                r.Status,
                r.HoldExpiresAt,
                r.ResolvedAt == default ? null : r.ResolvedAt))
            .ToList();

        var paginated = PaginatedList<ReservationDetailResponse>.Create(
            responseItems,
            totalCount,
            query.Page,
            query.PageSize);

        return Result<PaginatedList<ReservationDetailResponse>>.Success(paginated);
    }

    private async Task<IReadOnlyDictionary<Guid, (Guid ProductId, string ProductName)>> GetProductInfoAsync(
        IEnumerable<Domain.Entities.StockReservation> items,
        CancellationToken cancellationToken)
    {
        var batchIds = items.Select(r => r.BatchId).Distinct().ToList();

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

    private async Task<IReadOnlyDictionary<Guid, string>> GetStoreNamesAsync(
        IEnumerable<Domain.Entities.StockReservation> items,
        CancellationToken cancellationToken)
    {
        var storeIds = items
            .Select(r => r.RequestingStoreId)
            .Concat(items.Select(r => r.OwningStoreId))
            .Distinct()
            .ToList();

        if (storeIds.Count == 0)
        {
            return new Dictionary<Guid, string>();
        }

        var stores = await _storeRepository.GetByIdsAsync(storeIds, cancellationToken);

        return stores.ToDictionary(s => s.Id, s => s.Name);
    }
}